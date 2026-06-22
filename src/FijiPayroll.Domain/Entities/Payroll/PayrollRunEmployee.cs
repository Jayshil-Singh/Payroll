using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Shared.Guards;
using System;
using System.Collections.Generic;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Immutable snapshot record of an employee's calculation results within a payroll run.
/// Marks past calculation results as superseded rather than performing destructive physical deletes.
/// </summary>
public sealed class PayrollRunEmployee : BaseEntity
{
    private string _employeeName = string.Empty;
    private string _tin = string.Empty;
    private string _fnpfNumber = string.Empty;
    private string _residencyStatus = string.Empty;
    private string _department = string.Empty;
    private string _taxVersionUsed = string.Empty;

    private readonly List<PayrollRunLineItem> _lineItems = [];

    private PayrollRunEmployee() { }

    /// <summary>
    /// Foreign key to the PayrollRun header.
    /// </summary>
    public int PayrollRunId { get; private set; }

    /// <summary>Navigation to parent payroll run for tenant-scoped queries.</summary>
    public PayrollRun PayrollRun { get; private set; } = null!;

    /// <summary>
    /// Reference identifier for the employee.
    /// </summary>
    public int EmployeeId { get; private set; }

    /// <summary>
    /// Frozen display name of the employee at calculation time.
    /// </summary>
    public string EmployeeName
    {
        get => _employeeName;
        private set => _employeeName = Guard.AgainstNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Frozen Tax Identification Number of the employee.
    /// </summary>
    public string Tin
    {
        get => _tin;
        private set => _tin = value ?? string.Empty;
    }

    /// <summary>
    /// Frozen FNPF registration number.
    /// </summary>
    public string FnpfNumber
    {
        get => _fnpfNumber;
        private set => _fnpfNumber = value ?? string.Empty;
    }

    /// <summary>
    /// Residency status utilized for progressive tax calculation ("Resident", "NonResident").
    /// </summary>
    public string ResidencyStatus
    {
        get => _residencyStatus;
        private set => _residencyStatus = Guard.AgainstNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Department of the employee at calculation time.
    /// </summary>
    public string Department
    {
        get => _department;
        private set => _department = value ?? string.Empty;
    }

    /// <summary>Base pay/salary structure rate recorded.</summary>
    public decimal BaseSalary { get; private set; }

    /// <summary>Calculated gross pay.</summary>
    public decimal GrossPay { get; private set; }

    /// <summary>Sum of all allowances.</summary>
    public decimal TotalAllowances { get; private set; }

    /// <summary>Sum of all deductions.</summary>
    public decimal TotalDeductions { get; private set; }

    /// <summary>Take-home Net Pay amount.</summary>
    public decimal NetPay { get; private set; }

    /// <summary>Computed PAYE income tax.</summary>
    public decimal PayeTax { get; private set; }

    /// <summary>Computed FNPF employee portion contribution (typically 8%).</summary>
    public decimal FnpfEmployeeContribution { get; private set; }

    /// <summary>Computed FNPF employer portion contribution (typically 10%).</summary>
    public decimal FnpfEmployerContribution { get; private set; }

    /// <summary>The tax table rule version applied.</summary>
    public string TaxVersionUsed
    {
        get => _taxVersionUsed;
        private set => _taxVersionUsed = Guard.AgainstNullOrWhiteSpace(value);
    }

    /// <summary>
    /// If true, this calculation record has been superseded by a subsequent recalculation attempt.
    /// Excluded from standard reports/slips.
    /// </summary>
    public bool IsSuperseded { get; private set; }

    /// <summary>
    /// Request correlation ID to ensure idempotency.
    /// </summary>
    public Guid CalculationRequestId { get; private set; }

    /// <summary>
    /// Steps trace for audit debugging.
    /// </summary>
    public PayrollRunEmployeeTrace? Trace { get; private set; }

    /// <summary>
    /// Detailed calculation component line items.
    /// </summary>
    public IReadOnlyCollection<PayrollRunLineItem> LineItems => _lineItems.AsReadOnly();

    /// <summary>
    /// Marks the record as superseded due to a recalculation.
    /// </summary>
    public void SetSuperseded()
    {
        IsSuperseded = true;
    }

    /// <summary>
    /// Appends a calculation line item.
    /// </summary>
    public void AddLineItem(PayrollRunLineItem lineItem)
    {
        Guard.AgainstNull(lineItem);
        _lineItems.Add(lineItem);
    }

    /// <summary>
    /// Links the step trace.
    /// </summary>
    public void SetTrace(PayrollRunEmployeeTrace trace)
    {
        Guard.AgainstNull(trace);
        Trace = trace;
    }

    /// <summary>
    /// Factory method to create a new run employee details snap.
    /// </summary>
    public static PayrollRunEmployee Create(
        int payrollRunId,
        int employeeId,
        string employeeName,
        string tin,
        string fnpfNumber,
        string residencyStatus,
        string department,
        decimal baseSalary,
        decimal grossPay,
        decimal totalAllowances,
        decimal totalDeductions,
        decimal netPay,
        decimal payeTax,
        decimal fnpfEmployeeContribution,
        decimal fnpfEmployerContribution,
        string taxVersionUsed,
        Guid calculationRequestId)
    {
        return new PayrollRunEmployee
        {
            PayrollRunId = payrollRunId,
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            Tin = tin,
            FnpfNumber = fnpfNumber,
            ResidencyStatus = residencyStatus,
            Department = department,
            BaseSalary = baseSalary,
            GrossPay = grossPay,
            TotalAllowances = totalAllowances,
            TotalDeductions = totalDeductions,
            NetPay = netPay,
            PayeTax = payeTax,
            FnpfEmployeeContribution = fnpfEmployeeContribution,
            FnpfEmployerContribution = fnpfEmployerContribution,
            TaxVersionUsed = taxVersionUsed,
            IsSuperseded = false,
            CalculationRequestId = calculationRequestId
        };
    }
}
