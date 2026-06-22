namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Represents the company-wide policy governing voluntary deductions when net pay is insufficient.
/// </summary>
public enum NegativeNetPayPolicy
{
    /// <summary>Block deduction: Roll back calculation or abort run if net pay drops below zero.</summary>
    BlockDeduction = 0,

    /// <summary>Partial deduction: Scale down voluntary deduction to make net pay exactly zero, audit-logging warning flags.</summary>
    PartialDeduction = 1,

    /// <summary>Allow negative net pay: Deduct the full amount, allowing net pay to go negative (not standard for pilot, but supported by configuration).</summary>
    AllowNegativeNetPay = 2
}
