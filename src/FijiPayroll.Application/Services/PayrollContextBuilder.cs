using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Entities.Payroll;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Canonical builder for immutable <see cref="PayrollExecutionContext"/> snapshots.
/// Single source of truth for payroll input assembly.
/// </summary>
public sealed class PayrollContextBuilder
{
    private readonly IUnitOfWork _unitOfWork;

    public PayrollContextBuilder(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Builds a full execution context from pre-fetched component snapshots, tax brackets, and FNPF rates.
    /// This method is thread-safe and database-free.
    /// </summary>
    public PayrollExecutionContext Build(
        PayrollRun run,
        Guid calculationRequestId,
        IReadOnlyList<EmployeeSnapshot> employeeSnapshots,
        IReadOnlyList<PayrollComponentSnapshot> componentSnapshots,
        IReadOnlyList<TaxBracket> taxBrackets,
        decimal fnpfEmployeeRate,
        decimal fnpfEmployerRate,
        VoluntaryDeductionPolicy voluntaryDeductionPolicy,
        FijiPayroll.Domain.Entities.Company.NegativeNetPayPolicy negativeNetPayPolicy)
    {
        return new PayrollExecutionContext
        {
            PayrollRunId = run.Id,
            CompanyId = run.CompanyId,
            RunCode = run.RunCode,
            PeriodName = run.PeriodName,
            StartDate = run.StartDate,
            EndDate = run.EndDate,
            Frequency = run.Frequency,
            TaxVersion = FijiTaxConstants.CurrentTaxVersion,
            CalculationRequestId = calculationRequestId,
            Employees = employeeSnapshots,
            TaxRules = taxBrackets,
            Components = componentSnapshots,
            VoluntaryDeductionPolicy = voluntaryDeductionPolicy,
            NegativeNetPayPolicy = negativeNetPayPolicy,
            FnpfEmployeeRate = fnpfEmployeeRate,
            FnpfEmployerRate = fnpfEmployerRate
        };
    }

    /// <summary>
    /// Builds a full execution context for a payroll run including statutory rates and component snapshots.
    /// </summary>
    public async Task<PayrollExecutionContext> BuildAsync(
        PayrollRun run,
        Guid calculationRequestId,
        IReadOnlyList<EmployeeSnapshot> employeeSnapshots,
        VoluntaryDeductionPolicy voluntaryDeductionPolicy,
        CancellationToken cancellationToken = default)
    {
        var components = await _unitOfWork.PayrollComponents.GetByCompanyAsync(run.CompanyId, cancellationToken);
        var taxBrackets = await _unitOfWork.TaxBrackets.GetBracketsByVersionAndFrequencyAsync(
            FijiTaxConstants.CurrentTaxVersion,
            run.Frequency,
            cancellationToken);

        var (employeeRate, employerRate) = await ResolveFnpfRatesAsync(run.CompanyId, cancellationToken);
        var company = await _unitOfWork.Setup.GetCompanyByIdAsync(run.CompanyId, cancellationToken);
        var netPayPolicy = company?.NegativeNetPayPolicy ?? FijiPayroll.Domain.Entities.Company.NegativeNetPayPolicy.PartialDeduction;

        return new PayrollExecutionContext
        {
            PayrollRunId = run.Id,
            CompanyId = run.CompanyId,
            RunCode = run.RunCode,
            PeriodName = run.PeriodName,
            StartDate = run.StartDate,
            EndDate = run.EndDate,
            Frequency = run.Frequency,
            TaxVersion = FijiTaxConstants.CurrentTaxVersion,
            CalculationRequestId = calculationRequestId,
            Employees = employeeSnapshots,
            TaxRules = taxBrackets,
            Components = MapComponents(components),
            VoluntaryDeductionPolicy = voluntaryDeductionPolicy,
            NegativeNetPayPolicy = netPayPolicy,
            FnpfEmployeeRate = employeeRate,
            FnpfEmployerRate = employerRate
        };
    }

    /// <summary>
    /// Builds employee snapshots from domain employees, optionally loading staged adjustments.
    /// </summary>
    public async Task<IReadOnlyList<EmployeeSnapshot>> BuildEmployeeSnapshotsAsync(
        int companyId,
        IReadOnlyList<Employee> employees,
        IReadOnlyList<PayrollComponentSnapshot> components,
        bool includeAdjustments,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default)
    {
        var snapshots = new List<EmployeeSnapshot>();

        // Load all approved leave requests for the company that overlap with the period
        var leaveRequests = await _unitOfWork.Leave.GetRequestsByCompanyAsync(companyId, cancellationToken);
        var activeRequests = leaveRequests
            .Where(r => r.Status == FijiPayroll.Domain.Enumerations.LeaveStatus.Approved
                && r.StartDate <= periodEnd && r.EndDate >= periodStart)
            .ToList();

        // Load all active loans for the company
        var loans = await _unitOfWork.Loans.GetLoansByCompanyAsync(companyId, cancellationToken);
        var activeLoansMap = loans
            .Where(l => l.Status == FijiPayroll.Domain.Enumerations.LoanStatus.Active && l.RemainingBalance > 0)
            .GroupBy(l => l.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var emp in employees)
        {
            IReadOnlyList<EmployeeComponentOverrideSnapshot> overrides = Array.Empty<EmployeeComponentOverrideSnapshot>();

            if (includeAdjustments)
            {
                var adjustments = await _unitOfWork.PayrollAdjustments.GetUnappliedByEmployeeAsync(
                    companyId, emp.Id, cancellationToken);

                overrides = adjustments.Select(a =>
                {
                    string code = a.Description.Trim().ToUpperInvariant();
                    bool isValidComponent = components.Any(c =>
                        c.ComponentCode.Equals(code, StringComparison.OrdinalIgnoreCase));

                    if (!isValidComponent)
                    {
                        code = a.Type switch
                        {
                            PayrollAdjustmentType.Allowance => "TALLOWANCE",
                            PayrollAdjustmentType.Bonus => "BONUS",
                            PayrollAdjustmentType.Earning => "BONUS",
                            PayrollAdjustmentType.LeaveAdjustment => "BONUS",
                            PayrollAdjustmentType.BackPay => "BONUS",
                            PayrollAdjustmentType.RetroPay => "BONUS",
                            _ => a.Type.ToString().ToUpperInvariant()
                        };
                    }

                    return new EmployeeComponentOverrideSnapshot
                    {
                        ComponentCode = code,
                        Value = a.Amount
                    };
                }).ToList().AsReadOnly();
            }

            // Calculate leave days in period
            var empRequests = activeRequests.Where(r => r.EmployeeId == emp.Id).ToList();
            
            decimal paidDays = 0;
            decimal unpaidDays = 0;
            decimal loadingDays = 0;

            foreach (var req in empRequests)
            {
                var overlapStart = req.StartDate > periodStart ? req.StartDate : periodStart;
                var overlapEnd = req.EndDate < periodEnd ? req.EndDate : periodEnd;
                
                decimal overlapWorkingDays = 0;
                for (var date = overlapStart.Date; date <= overlapEnd.Date; date = date.AddDays(1))
                {
                    if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                    {
                        overlapWorkingDays++;
                    }
                }
                
                decimal requestDaysInPeriod = Math.Min(overlapWorkingDays, req.TotalDays);

                if (req.LeaveType?.IsPaid == true)
                {
                    paidDays += requestDaysInPeriod;
                    if (req.LeaveType.ApplyLeaveLoading)
                    {
                        loadingDays += requestDaysInPeriod;
                    }
                }
                else
                {
                    unpaidDays += requestDaysInPeriod;
                }
            }

            // Active loans for employee
            IReadOnlyList<EmployeeLoanSnapshot> empLoans = activeLoansMap.TryGetValue(emp.Id, out var empLoanList)
                ? empLoanList.Select(l => new EmployeeLoanSnapshot
                {
                    LoanId = l.Id,
                    LoanDescription = l.LoanDescription,
                    RemainingBalance = l.RemainingBalance,
                    DeductionAmountPerPeriod = l.DeductionAmountPerPeriod
                }).ToList()
                : Array.Empty<EmployeeLoanSnapshot>();

            snapshots.Add(new EmployeeSnapshot
            {
                EmployeeId = emp.Id,
                FullName = emp.FullName,
                Tin = emp.Tin,
                FnpfNumber = emp.FnpfNumber,
                ResidencyStatus = emp.ResidencyStatus,
                Department = emp.Department,
                BaseSalary = emp.BaseSalary,
                IsFnpfExempt = emp.IsFnpfExempt,
                IsTaxExempt = emp.IsTaxExempt,
                HoursWorked = 40m,
                OvertimeHours = 0m,
                ComponentOverrides = overrides,
                PaidLeaveDays = paidDays,
                UnpaidLeaveDays = unpaidDays,
                LeaveLoadingDays = loadingDays,
                ActiveLoans = empLoans
            });
        }

        return snapshots.AsReadOnly();
    }

    private async Task<(decimal EmployeeRate, decimal EmployerRate)> ResolveFnpfRatesAsync(
        int companyId,
        CancellationToken cancellationToken)
    {
        var config = await _unitOfWork.Setup.GetActiveFnpfConfigurationAsync(companyId, cancellationToken);
        if (config != null)
        {
            return (config.EmployeeRate, config.EmployerRate);
        }

        return (FijiTaxConstants.DefaultFnpfEmployeeRate, FijiTaxConstants.DefaultFnpfEmployerRate);
    }

    private static IReadOnlyList<PayrollComponentSnapshot> MapComponents(
        IReadOnlyList<PayrollComponent> components)
    {
        return components.Select(c => new PayrollComponentSnapshot
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
    }
}
