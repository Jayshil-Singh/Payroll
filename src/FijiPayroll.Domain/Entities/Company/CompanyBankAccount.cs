using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using System;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain entity representing a company bank account used for operations, payroll, or tax payments.
/// </summary>
public sealed class CompanyBankAccount : SoftDeleteEntity
{
    private CompanyBankAccount() { }

    /// <summary>Gets the company tenant ID.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the account name.</summary>
    public string AccountName { get; private set; } = string.Empty;

    /// <summary>Gets the bank master ID.</summary>
    public int BankMasterId { get; private set; }

    /// <summary>Gets the bank branch ID.</summary>
    public int BankBranchId { get; private set; }

    /// <summary>Gets the type of bank account.</summary>
    public BankAccountType AccountType { get; private set; }

    /// <summary>Gets the encrypted account number (with prepended metadata format: [Algorithm]:[KeyVersion]:[KeyIdentifier]:[CipherText]).</summary>
    public string EncryptedAccountNumber { get; private set; } = string.Empty;

    /// <summary>Gets the hash of the account number for duplicate detection.</summary>
    public string AccountNumberHash { get; private set; } = string.Empty;

    /// <summary>Gets the last 4 digits of the account number for UI display.</summary>
    public string Last4Digits { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether this account is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Factory method to create a new CompanyBankAccount.</summary>
    public static CompanyBankAccount Create(
        int companyId,
        string accountName,
        int bankMasterId,
        int bankBranchId,
        BankAccountType accountType,
        string encryptedAccountNumber,
        string accountNumberHash,
        string last4Digits,
        bool isActive = true)
    {
        if (companyId <= 0)
            throw new ArgumentException("Company ID must be positive.", nameof(companyId));
        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("Account name is required.", nameof(accountName));
        if (bankMasterId <= 0)
            throw new ArgumentException("Bank master ID must be positive.", nameof(bankMasterId));
        if (bankBranchId <= 0)
            throw new ArgumentException("Bank branch ID must be positive.", nameof(bankBranchId));
        if (string.IsNullOrWhiteSpace(encryptedAccountNumber))
            throw new ArgumentException("Encrypted account number is required.", nameof(encryptedAccountNumber));
        if (string.IsNullOrWhiteSpace(accountNumberHash))
            throw new ArgumentException("Account number hash is required.", nameof(accountNumberHash));
        if (string.IsNullOrWhiteSpace(last4Digits) || last4Digits.Length > 4)
            throw new ArgumentException("Last 4 digits must be valid and at most 4 characters.", nameof(last4Digits));

        return new CompanyBankAccount
        {
            CompanyId = companyId,
            AccountName = accountName,
            BankMasterId = bankMasterId,
            BankBranchId = bankBranchId,
            AccountType = accountType,
            EncryptedAccountNumber = encryptedAccountNumber,
            AccountNumberHash = accountNumberHash,
            Last4Digits = last4Digits,
            IsActive = isActive
        };
    }

    /// <summary>Gets a masked version of the account number for secure UI display.</summary>
    public string GetMaskedAccountNumber()
    {
        return $"******{Last4Digits}";
    }

    /// <summary>Updates bank account information.</summary>
    public void Update(
        string accountName,
        int bankMasterId,
        int bankBranchId,
        BankAccountType accountType,
        string encryptedAccountNumber,
        string accountNumberHash,
        string last4Digits,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("Account name is required.", nameof(accountName));
        if (bankMasterId <= 0)
            throw new ArgumentException("Bank master ID must be positive.", nameof(bankMasterId));
        if (bankBranchId <= 0)
            throw new ArgumentException("Bank branch ID must be positive.", nameof(bankBranchId));
        if (string.IsNullOrWhiteSpace(encryptedAccountNumber))
            throw new ArgumentException("Encrypted account number is required.", nameof(encryptedAccountNumber));
        if (string.IsNullOrWhiteSpace(accountNumberHash))
            throw new ArgumentException("Account number hash is required.", nameof(accountNumberHash));
        if (string.IsNullOrWhiteSpace(last4Digits) || last4Digits.Length > 4)
            throw new ArgumentException("Last 4 digits must be valid and at most 4 characters.", nameof(last4Digits));

        AccountName = accountName;
        BankMasterId = bankMasterId;
        BankBranchId = bankBranchId;
        AccountType = accountType;
        EncryptedAccountNumber = encryptedAccountNumber;
        AccountNumberHash = accountNumberHash;
        Last4Digits = last4Digits;
        IsActive = isActive;
    }
}
