namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Status of an employee staff loan.
/// </summary>
public enum LoanStatus
{
    /// <summary>Loan is active and deductions are applied.</summary>
    Active,

    /// <summary>Loan has been fully repaid.</summary>
    FullyPaid,

    /// <summary>Loan deductions are suspended temporarily.</summary>
    Suspended,

    /// <summary>Loan has been cancelled or written off.</summary>
    WrittenOff
}
