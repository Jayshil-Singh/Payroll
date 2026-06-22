using FijiPayroll.Domain.Enumerations;
using System;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Owned entity representing an employee's payment method configuration.
/// Supports cash, cheque, bank transfer, and mobile transfer.
/// </summary>
public sealed class EmployeePaymentMethod
{
    private string? _bankName;
    private string? _bankAccountNumber;
    private string? _bankSortCode;
    private string? _mobileNumber;

    private EmployeePaymentMethod() { }

    /// <summary>Gets the payment method type.</summary>
    public PaymentMethodType MethodType { get; private set; }

    /// <summary>Gets the bank name.</summary>
    public string? BankName
    {
        get => _bankName;
        private set => _bankName = value;
    }

    /// <summary>Gets the bank account number.</summary>
    public string? BankAccountNumber
    {
        get => _bankAccountNumber;
        private set => _bankAccountNumber = value;
    }

    /// <summary>Gets the bank sort/routing code.</summary>
    public string? BankSortCode
    {
        get => _bankSortCode;
        private set => _bankSortCode = value;
    }

    /// <summary>Gets the mobile number (for M-PAiSA or MyCash).</summary>
    public string? MobileNumber
    {
        get => _mobileNumber;
        private set => _mobileNumber = value;
    }

    /// <summary>Gets the allocation percentage (e.g., 100 for single payment, or split).</summary>
    public decimal Percentage { get; private set; }

    /// <summary>Gets a value indicating whether this is the primary payment method.</summary>
    public bool IsPrimary { get; private set; }

    /// <summary>Deactivates the payment method by clearing primary flag and resetting percentage.</summary>
    public void Deactivate()
    {
        IsPrimary = false;
        Percentage = 0;
    }

    /// <summary>Factory method to create an EmployeePaymentMethod.</summary>
    public static EmployeePaymentMethod Create(
        PaymentMethodType methodType,
        decimal percentage,
        bool isPrimary,
        string? bankName = null,
        string? bankAccountNumber = null,
        string? bankSortCode = null,
        string? mobileNumber = null)
    {
        if (percentage <= 0 || percentage > 100)
            throw new ArgumentException("Percentage must be between 1 and 100.", nameof(percentage));

        if (methodType == PaymentMethodType.BankTransfer)
        {
            if (string.IsNullOrWhiteSpace(bankName))
                throw new ArgumentException("Bank name is required for bank transfer.", nameof(bankName));
            if (string.IsNullOrWhiteSpace(bankAccountNumber))
                throw new ArgumentException("Bank account number is required for bank transfer.", nameof(bankAccountNumber));
        }
        else if (methodType == PaymentMethodType.MobileTransfer)
        {
            if (string.IsNullOrWhiteSpace(mobileNumber))
                throw new ArgumentException("Mobile number is required for mobile transfer.", nameof(mobileNumber));
        }

        return new EmployeePaymentMethod
        {
            MethodType = methodType,
            Percentage = percentage,
            IsPrimary = isPrimary,
            BankName = bankName,
            BankAccountNumber = bankAccountNumber,
            BankSortCode = bankSortCode,
            MobileNumber = mobileNumber
        };
    }
}
