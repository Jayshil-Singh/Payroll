namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Defines the payment method types supported by the payroll system.
/// </summary>
public enum PaymentMethodType
{
    /// <summary>Cash payment.</summary>
    Cash = 1,

    /// <summary>Cheque payment.</summary>
    Cheque = 2,

    /// <summary>Direct bank transfer (EFT).</summary>
    BankTransfer = 3,

    /// <summary>Mobile transfer (M-PAiSA, MyCash).</summary>
    MobileTransfer = 4
}
