using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Services;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.PayrollRuns.Commands.CalculatePayrollRun;

public sealed record CalculatePayrollRunCommand(
    int PayrollRunId,
    Guid CalculationRequestId
) : IRequest<Result>;

public sealed class CalculatePayrollRunCommandHandler : IRequestHandler<CalculatePayrollRunCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly PayrollCalculationEngine _calculationEngine;
    private readonly PayrollValidationService _validationService;

    public CalculatePayrollRunCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        PayrollCalculationEngine calculationEngine,
        PayrollValidationService validationService)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _calculationEngine = calculationEngine;
        _validationService = validationService;
    }

    public async Task<Result> Handle(CalculatePayrollRunCommand request, CancellationToken cancellationToken)
    {
        if (ResetOperationContext.IsResetting)
        {
            return Result.Failure("Calculation cannot be initiated during a reset operation.");
        }

        if (!_currentUser.HasPermission(PermissionConstants.PayrollRunsEdit))
        {
            return Result.Failure("Forbidden: You do not have permission to calculate payroll runs.");
        }

        // 1. Retrieve the payroll run header
        var run = await _unitOfWork.PayrollRuns.GetByIdWithDetailsAsync(request.PayrollRunId, cancellationToken);
        if (run == null)
        {
            return Result.Failure($"Payroll run with ID {request.PayrollRunId} was not found.");
        }

        if (!_currentUser.HasCompanyAccess(run.CompanyId))
        {
            return Result.Failure("Forbidden: You do not have access to this company's payroll runs.");
        }

        // 2. Query the inputs for context (ORDERING RULE: DB-level sorted ORDER BY EmployeeId)
        var employees = await _unitOfWork.Employees.GetByCompanyAndFrequencyAsync(
            run.CompanyId,
            run.Frequency,
            cancellationToken);

        var components = await _unitOfWork.PayrollComponents.GetByCompanyAsync(
            run.CompanyId,
            cancellationToken);

        string taxVersion = "2025-2026"; // Default standard tax table rules version
        var taxBrackets = await _unitOfWork.TaxBrackets.GetBracketsByVersionAndFrequencyAsync(
            taxVersion,
            run.Frequency,
            cancellationToken);

        // 3. Build immutable PayrollExecutionContext
        var context = new PayrollExecutionContext
        {
            PayrollRunId = run.Id,
            CompanyId = run.CompanyId,
            RunCode = run.RunCode,
            PeriodName = run.PeriodName,
            StartDate = run.StartDate,
            EndDate = run.EndDate,
            Frequency = run.Frequency,
            TaxVersion = taxVersion,
            CalculationRequestId = request.CalculationRequestId,
            Employees = employees.Select(e => new EmployeeSnapshot
            {
                EmployeeId = e.Id,
                FullName = e.FullName,
                Tin = e.Tin,
                FnpfNumber = e.FnpfNumber,
                ResidencyStatus = e.ResidencyStatus,
                Department = e.Department,
                BaseSalary = e.BaseSalary,
                IsFnpfExempt = e.IsFnpfExempt,
                IsTaxExempt = e.IsTaxExempt,
                HoursWorked = 40m, // Default standard hours worked in Fiji period
                OvertimeHours = 0m,
                ComponentOverrides = Array.Empty<EmployeeComponentOverrideSnapshot>()
            }).ToList().AsReadOnly(),
            TaxRules = taxBrackets,
            Components = components.Select(c => new PayrollComponentSnapshot
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
            }).ToList().AsReadOnly()
        };

        // 4. Validate the context BEFORE lock acquisition (entry boundary verification)
        _validationService.Validate(context);
        PayrollExecutionContractValidator.Validate(context);

        // 5. Acquire lock (Status = Calculating) via repository
        bool lockAcquired = await _unitOfWork.PayrollRuns.AcquireLockAsync(
            request.PayrollRunId,
            request.CalculationRequestId,
            _currentUser.Username,
            cancellationToken);

        if (!lockAcquired)
        {
            return Result.Failure("Cannot calculate payroll run: it is currently locked by another calculation process, or is already being processed, or is in an invalid status.");
        }

        try
        {
            // 6. Execute calculation (pure orchestrator)
            var results = _calculationEngine.Calculate(context);

            // Mark previous calculations as superseded
            foreach (var existingEmp in run.Employees.Where(e => !e.IsSuperseded))
            {
                existingEmp.SetSuperseded();
            }

            // 7. Map calculation results snapshot back to entities
            foreach (var res in results.Employees)
            {
                var runEmp = PayrollRunEmployee.Create(
                    run.Id,
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
                    request.CalculationRequestId
                );

                foreach (var line in res.LineItems)
                {
                    var runLine = PayrollRunLineItem.Create(
                        0, // EF core auto-relates when added to employee list
                        line.ComponentId,
                        line.ComponentCode,
                        line.ComponentName,
                        line.ComponentType,
                        line.Amount,
                        line.IsTaxable,
                        line.AffectsFnpf,
                        line.EmployerContributionFlag,
                        line.ReferenceComponentId
                    );
                    runEmp.AddLineItem(runLine);
                }

                // Add immutable trace record
                var trace = PayrollRunEmployeeTrace.Create(0, res.TraceText);
                runEmp.SetTrace(trace);

                run.AddEmployee(runEmp);
            }

            // 8. Release lock to Calculated
            run.ReleaseLockToCalculated(results.SnapshotHash, _currentUser.Username);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            // SAFEGUARD: If calculation fails, release lock to Draft, save and re-throw
            try
            {
                // Reload and rollback status to draft
                var reloadRun = await _unitOfWork.PayrollRuns.GetByIdAsync(request.PayrollRunId, cancellationToken);
                if (reloadRun != null && reloadRun.Status == PayrollRunStatus.Calculating)
                {
                    reloadRun.ReleaseLockToDraft(_currentUser.Username, ex.Message);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
            catch
            {
                // Ignore silent sub-exception during rollback to avoid masking primary error
            }

            throw; // Re-throw primary calculation exception
        }
    }
}
