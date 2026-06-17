namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Defines the specific business purpose and classification of a company bank account.
/// </summary>
public enum BankAccountType
{
    /// <summary>Account used for general business operations and payments.</summary>
    Operating = 1,

    /// <summary>Account dedicated exclusively for employee salary disbursements.</summary>
    Payroll = 2,

    /// <summary>Account held for withholding tax and PAYE obligations.</summary>
    Tax = 3,

    /// <summary>Account held for statutory FNPF contributions remittance.</summary>
    FNPF = 4,

    /// <summary>General savings account holding company surplus funds.</summary>
    Savings = 5,

    /// <summary>Account held under fiduciary trust conditions.</summary>
    Trust = 6,

    /// <summary>Custom account type for specialised payment allocations.</summary>
    Custom = 7
}
