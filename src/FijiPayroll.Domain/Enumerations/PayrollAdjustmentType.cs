namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Defines categories for staging payroll adjustments.
/// </summary>
public enum PayrollAdjustmentType
{
    Earning = 1,
    Allowance = 2,
    Deduction = 3,
    TaxAdjustment = 4,
    FNPFAdjustment = 5,
    BackPay = 6,
    RetroPay = 7,
    Bonus = 8,
    LeaveAdjustment = 9,
    LoanRecovery = 10,
    SalaryAdvance = 11
}
