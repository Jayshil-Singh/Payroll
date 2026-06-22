using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Canonical payroll execution pipeline. All calculation entry points MUST delegate here.
/// </summary>
public sealed class PayrollPipelineService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly PayrollContextBuilder _contextBuilder;
    private readonly PayrollCalculationEngine _calculationEngine;
    private readonly PayrollValidationService _validationService;
    private readonly PayrollValidationPipeline _validationPipeline;
    private readonly BatchProcessingCoordinator _coordinator;

    public PayrollPipelineService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        PayrollContextBuilder contextBuilder,
        PayrollCalculationEngine calculationEngine,
        PayrollValidationService validationService,
        PayrollValidationPipeline validationPipeline,
        BatchProcessingCoordinator coordinator)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _contextBuilder = contextBuilder;
        _calculationEngine = calculationEngine;
        _validationService = validationService;
        _validationPipeline = validationPipeline;
        _coordinator = coordinator;
    }

    private static readonly SemaphoreSlim _concurrencySemaphore = new(1, 1);

    /// <summary>
    /// Executes the full payroll pipeline: validate → lock → calculate → persist → snapshot → audit.
    /// </summary>
    public async Task<Result> ExecuteAsync(
        int payrollRunId,
        Guid calculationRequestId,
        CancellationToken cancellationToken = default)
    {
        await _concurrencySemaphore.WaitAsync(cancellationToken);
        try
        {
            if (ResetOperationContext.IsResetting)
        {
            return Result.Failure("Calculation cannot be initiated during a reset operation.");
        }

        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsEdit))
        {
            return Result.Failure("Forbidden: You do not have permission to calculate payroll runs.");
        }

        var run = await _unitOfWork.PayrollRuns.GetByIdWithDetailsAsync(payrollRunId, cancellationToken);
        if (run == null)
        {
            return Result.Failure($"Payroll run with ID {payrollRunId} was not found.");
        }

        if (!_currentUser.HasCompanyAccess(run.CompanyId))
        {
            return Result.Failure("Forbidden: You do not have access to this company's payroll runs.");
        }

        var batchState = _coordinator.GetOrCreateState(run.Id);
        batchState.Progress = 0;
        batchState.IsPaused = false;

        bool lockAcquired = await _unitOfWork.PayrollRuns.AcquireLockAsync(
            payrollRunId,
            calculationRequestId,
            _currentUser.Username,
            cancellationToken);

        if (!lockAcquired)
        {
            _coordinator.RemoveState(run.Id);
            return Result.Failure("Cannot calculate payroll run: it is currently locked or being processed.");
        }

        try
        {
            await ClearPriorResultsAsync(run, cancellationToken);

            var employees = await _unitOfWork.Employees.GetByCompanyAndFrequencyAsync(
                run.CompanyId,
                run.Frequency,
                cancellationToken);

            var components = await _unitOfWork.PayrollComponents.GetByCompanyAsync(
                run.CompanyId,
                cancellationToken);

            var componentSnapshots = components.Select(c => new PayrollComponentSnapshot
            {
                Id = c.Id,
                ComponentCode = c.ComponentCode,
                ComponentName = c.ComponentName,
                ComponentType = c.ComponentType,
                CalculationMethod = c.CalculationMethod,
                CalculationValue = c.CalculationValue,
                Formula = c.Formula,
                IsTaxable = c.IsTaxable,
                IsFnpfApplicable = c.IsFnpfApplicable,
                DisplayOrder = c.DisplayOrder
            }).ToList().AsReadOnly();

            var validationIssues = _validationPipeline.Validate(
                run.CompanyId, employees, FijiTaxConstants.CurrentTaxVersion, "EngineV1");

            var globalCriticalErrors = validationIssues
                .Where(i => i.EmployeeId == null && i.Severity == PayrollValidationSeverity.Critical)
                .ToList();

            if (globalCriticalErrors.Any())
            {
                run.ReleaseLockToDraft(_currentUser.Username, $"Global Validation Failure: {globalCriticalErrors.First().Message}");
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Failure($"Validation Blocked: {globalCriticalErrors[0].Message}");
            }

            var employeeIssueMap = validationIssues
                .Where(i => i.EmployeeId.HasValue)
                .GroupBy(i => i.EmployeeId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var validEmployees = new List<FijiPayroll.Domain.Entities.Company.Employee>();
            foreach (var emp in employees)
            {
                if (employeeIssueMap.TryGetValue(emp.Id, out var issues) &&
                    issues.Any(i => i.Severity == PayrollValidationSeverity.Critical ||
                                      i.Severity == PayrollValidationSeverity.Error))
                {
                    var primaryIssue = issues.First(i =>
                        i.Severity == PayrollValidationSeverity.Critical ||
                        i.Severity == PayrollValidationSeverity.Error);

                    await _unitOfWork.PayrollExceptionQueues.AddAsync(
                        PayrollExceptionQueue.Create(
                            run.CompanyId,
                            run.Id,
                            emp.Id,
                            emp.FullName,
                            primaryIssue.Message,
                            primaryIssue.Severity,
                            primaryIssue.Recommendation,
                            null,
                            Guid.NewGuid().ToString()),
                        cancellationToken);
                }
                else
                {
                    validEmployees.Add(emp);
                }
            }

            if (validEmployees.Count == 0)
            {
                run.ReleaseLockToCalculated("EMPTY_RUN_HASH", _currentUser.Username);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }

            var company = await _unitOfWork.Setup.GetCompanyByIdAsync(run.CompanyId, cancellationToken);
            var netPayPolicy = company?.NegativeNetPayPolicy ?? FijiPayroll.Domain.Entities.Company.NegativeNetPayPolicy.PartialDeduction;

            var taxBrackets = await _unitOfWork.TaxBrackets.GetBracketsByVersionAndFrequencyAsync(
                FijiTaxConstants.CurrentTaxVersion,
                run.Frequency,
                cancellationToken);

            decimal fnpfEmployeeRate = FijiTaxConstants.DefaultFnpfEmployeeRate;
            decimal fnpfEmployerRate = FijiTaxConstants.DefaultFnpfEmployerRate;
            var fnpfConfig = await _unitOfWork.Setup.GetActiveFnpfConfigurationAsync(run.CompanyId, cancellationToken);
            if (fnpfConfig != null)
            {
                fnpfEmployeeRate = fnpfConfig.EmployeeRate;
                fnpfEmployerRate = fnpfConfig.EmployerRate;
            }

            var validSnapshots = await _contextBuilder.BuildEmployeeSnapshotsAsync(
                run.CompanyId,
                validEmployees,
                componentSnapshots,
                includeAdjustments: true,
                run.StartDate,
                run.EndDate,
                cancellationToken);

            if (validSnapshots.Count > 0)
            {
                var preflightContext = _contextBuilder.Build(
                    run,
                    calculationRequestId,
                    validSnapshots,
                    componentSnapshots,
                    taxBrackets,
                    fnpfEmployeeRate,
                    fnpfEmployerRate,
                    VoluntaryDeductionPolicy.PartialDeductionWithAuditFlag,
                    netPayPolicy);

                _validationService.Validate(preflightContext);
                PayrollExecutionContractValidator.Validate(preflightContext);
            }

            var calculatedResults = new ConcurrentBag<CalculatedEmployeeResult>();
            var failedCalculations = new ConcurrentBag<(EmployeeSnapshot Employee, Exception Exception)>();
            int processedCount = 0;
            int totalCount = validSnapshots.Count;

            var processBlock = new ActionBlock<EmployeeSnapshot>(
                async empSnap =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await batchState.WaitIfPausedAsync(cancellationToken);

                    try
                    {
                        var singleContext = _contextBuilder.Build(
                            run,
                            calculationRequestId,
                            new List<EmployeeSnapshot> { empSnap }.AsReadOnly(),
                            componentSnapshots,
                            taxBrackets,
                            fnpfEmployeeRate,
                            fnpfEmployerRate,
                            VoluntaryDeductionPolicy.PartialDeductionWithAuditFlag,
                            netPayPolicy);

                        _validationService.Validate(singleContext);
                        PayrollExecutionContractValidator.Validate(singleContext);

                        var calculationResult = _calculationEngine.Calculate(singleContext);
                        var result = calculationResult.Employees.First();

                        if (result.IsSuccess)
                        {
                            calculatedResults.Add(result);
                        }
                        else
                        {
                            failedCalculations.Add((empSnap, new Exception(result.ErrorMessage ?? "Calculation returned failure status.")));
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCalculations.Add((empSnap, ex));
                    }
                    finally
                    {
                        var count = Interlocked.Increment(ref processedCount);
                        batchState.Progress = Math.Round((double)count / totalCount * 100, 2);
                    }
                },
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount),
                    CancellationToken = cancellationToken
                });

            foreach (var snap in validSnapshots)
            {
                processBlock.Post(snap);
            }

            processBlock.Complete();
            await processBlock.Completion;

            var successfullyCalculatedSnapshots = new List<EmployeeSnapshot>();

            foreach (var res in calculatedResults.OrderBy(r => r.EmployeeId))
            {
                var runEmp = CreateRunEmployee(run.Id, res, calculationRequestId);
                run.AddEmployee(runEmp);

                successfullyCalculatedSnapshots.Add(validSnapshots.First(s => s.EmployeeId == res.EmployeeId));

                var employeeAdjustments = await _unitOfWork.PayrollAdjustments.GetUnappliedByEmployeeAsync(
                    run.CompanyId, res.EmployeeId, cancellationToken);

                foreach (var adj in employeeAdjustments)
                {
                    adj.Apply(run.Id, _currentUser.Username);
                    _unitOfWork.PayrollAdjustments.Update(adj);
                }
            }

            foreach (var failed in failedCalculations)
            {
                await _unitOfWork.PayrollExceptionQueues.AddAsync(
                    PayrollExceptionQueue.Create(
                        run.CompanyId,
                        run.Id,
                        failed.Employee.EmployeeId,
                        failed.Employee.FullName,
                        failed.Exception.Message,
                        PayrollValidationSeverity.Error,
                        "Verify employee master details and adjustments.",
                        failed.Exception.StackTrace,
                        Guid.NewGuid().ToString()),
                    cancellationToken);
            }

            string snapshotHash = await PersistSnapshotAsync(
                run,
                calculationRequestId,
                successfullyCalculatedSnapshots,
                componentSnapshots,
                cancellationToken);

            run.ReleaseLockToCalculated(snapshotHash, _currentUser.Username);
            _unitOfWork.PayrollRuns.Update(run);

            await _unitOfWork.PayrollRunHistories.AddAsync(
                PayrollRunHistory.Create(
                    run.CompanyId,
                    run.Id,
                    "CalculatePipeline",
                    _currentUser.Username,
                    "Server",
                    calculationRequestId.ToString(),
                    $"Calculated payroll run via canonical pipeline. Successful: {calculatedResults.Count}, Failed: {failedCalculations.Count}",
                    null,
                    null),
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception)
        {
            try
            {
                var reloadRun = await _unitOfWork.PayrollRuns.GetByIdAsync(payrollRunId, cancellationToken);
                if (reloadRun != null && reloadRun.Status == PayrollRunStatus.Calculating)
                {
                    reloadRun.ReleaseLockToDraft(_currentUser.Username, "Calculation pipeline failed.");
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
            catch
            {
                // Preserve primary exception
            }

            throw;
        }
        finally
        {
            if (run != null)
            {
                _coordinator.RemoveState(run.Id);
            }
        }
        }
        finally
        {
            _concurrencySemaphore.Release();
        }
    }

    private async Task ClearPriorResultsAsync(PayrollRun run, CancellationToken cancellationToken)
    {
        var oldExceptions = await _unitOfWork.PayrollExceptionQueues.GetByRunIdAsync(run.Id, cancellationToken);
        foreach (var ex in oldExceptions)
        {
            _unitOfWork.PayrollExceptionQueues.Remove(ex);
        }

        foreach (var existingEmp in run.Employees.Where(e => !e.IsSuperseded))
        {
            existingEmp.SetSuperseded();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static PayrollRunEmployee CreateRunEmployee(
        int payrollRunId,
        CalculatedEmployeeResult res,
        Guid calculationRequestId)
    {
        var runEmp = PayrollRunEmployee.Create(
            payrollRunId,
            res.EmployeeId,
            res.EmployeeName,
            res.Tin,
            res.FnpfNumber,
            res.ResidencyStatus,
            res.Department,
            res.BaseSalary,
            res.GrossPay,
            res.TotalAllowances,
            res.TotalDeductions,
            res.NetPay,
            res.PayeTax,
            res.FnpfEmployeeContribution,
            res.FnpfEmployerContribution,
            res.TaxVersionUsed,
            calculationRequestId);

        foreach (var line in res.LineItems)
        {
            runEmp.AddLineItem(PayrollRunLineItem.Create(
                0,
                line.ComponentId,
                line.ComponentCode,
                line.ComponentName,
                line.ComponentType,
                line.Amount,
                line.IsTaxable,
                line.AffectsFnpf,
                line.EmployerContributionFlag,
                line.ReferenceComponentId));
        }

        runEmp.SetTrace(PayrollRunEmployeeTrace.Create(0, res.TraceText));
        return runEmp;
    }

    private async Task<string> PersistSnapshotAsync(
        PayrollRun run,
        Guid calculationRequestId,
        List<EmployeeSnapshot> successfullyCalculatedSnapshots,
        IReadOnlyList<PayrollComponentSnapshot> componentSnapshots,
        CancellationToken cancellationToken)
    {
        if (successfullyCalculatedSnapshots.Count == 0)
        {
            return "EMPTY_RUN_HASH";
        }

        var snapshotContext = await _contextBuilder.BuildAsync(
            run,
            calculationRequestId,
            successfullyCalculatedSnapshots.AsReadOnly(),
            VoluntaryDeductionPolicy.PartialDeductionWithAuditFlag,
            cancellationToken);

        string snapshotJson = JsonSerializer.Serialize(snapshotContext);
        string snapshotHash = PayrollSnapshotHasher.GenerateHash(
            snapshotContext.Employees,
            snapshotContext.TaxVersion,
            snapshotContext.Components);

        int version = 1;
        var existingSnapshots = await _unitOfWork.PayrollSnapshots.GetByRunIdAsync(run.Id, cancellationToken);
        if (existingSnapshots.Any())
        {
            version = existingSnapshots.Max(x => x.Version) + 1;
        }

        await _unitOfWork.PayrollSnapshots.AddAsync(
            PayrollSnapshot.Create(
                run.CompanyId,
                run.Id,
                version,
                snapshotJson,
                snapshotHash,
                _currentUser.Username),
            cancellationToken);

        return snapshotHash;
    }
}
