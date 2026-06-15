namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Defines the nature of a payroll component — whether it adds to or
/// subtracts from an employee's gross pay, and how it is treated for
/// tax and FNPF purposes.
/// </summary>
public enum ComponentType
{
    /// <summary>
    /// A monetary payment that forms part of gross earnings
    /// (e.g., Basic Salary, Overtime Pay).
    /// Typically taxable and FNPF-applicable unless specifically excluded.
    /// </summary>
    Earning = 1,

    /// <summary>
    /// A deduction subtracted from gross pay to arrive at net pay
    /// (e.g., Loan Repayment, Union Fees, Health Insurance).
    /// Not taxable (deductions reduce take-home, not tax base — PAYE is separate).
    /// </summary>
    Deduction = 2,

    /// <summary>
    /// An additional payment on top of base salary, commonly for living expenses
    /// (e.g., Housing Allowance, Transport Allowance, Meal Allowance).
    /// Taxability and FNPF applicability are component-specific per FRCS rules.
    /// </summary>
    Allowance = 3,

    /// <summary>
    /// A non-cash or in-kind benefit provided to an employee
    /// (e.g., Vehicle Benefit, Health Insurance Employer Contribution).
    /// May or may not be taxable depending on FRCS treatment.
    /// </summary>
    Benefit = 4,

    /// <summary>
    /// A legislatively mandated deduction or contribution that cannot be disabled
    /// (e.g., PAYE Tax, FNPF Employee Contribution, FNPF Employer Contribution).
    /// System components cannot be deleted or deactivated.
    /// </summary>
    Statutory = 5,
}
