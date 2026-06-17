using FijiPayroll.Domain.Entities.Common;
using System;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain entity representing a bank branch configuration under a BankMaster.
/// </summary>
public sealed class BankBranch : SoftDeleteEntity
{
    private BankBranch() { }

    /// <summary>Gets the company tenant ID.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the parent BankMaster ID.</summary>
    public int BankMasterId { get; private set; }

    /// <summary>Gets the branch code.</summary>
    public string BranchCode { get; private set; } = string.Empty;

    /// <summary>Gets the branch name.</summary>
    public string BranchName { get; private set; } = string.Empty;

    /// <summary>Gets the BSB code (Bank-State-Branch format, standard in Fiji payments).</summary>
    public string BsbCode { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether this branch is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets or sets the parent bank master entity.</summary>
    public BankMaster? BankMaster { get; private set; }

    /// <summary>Factory method to create a new BankBranch.</summary>
    public static BankBranch Create(
        int companyId,
        int bankMasterId,
        string branchCode,
        string branchName,
        string bsbCode,
        bool isActive = true)
    {
        if (companyId <= 0)
            throw new ArgumentException("Company ID must be positive.", nameof(companyId));
        if (bankMasterId <= 0)
            throw new ArgumentException("Bank master ID must be positive.", nameof(bankMasterId));
        if (string.IsNullOrWhiteSpace(branchCode))
            throw new ArgumentException("Branch code cannot be empty.", nameof(branchCode));
        if (string.IsNullOrWhiteSpace(branchName))
            throw new ArgumentException("Branch name cannot be empty.", nameof(branchName));
        if (string.IsNullOrWhiteSpace(bsbCode))
            throw new ArgumentException("BSB code cannot be empty.", nameof(bsbCode));

        return new BankBranch
        {
            CompanyId = companyId,
            BankMasterId = bankMasterId,
            BranchCode = branchCode,
            BranchName = branchName,
            BsbCode = bsbCode,
            IsActive = isActive
        };
    }

    /// <summary>Updates the bank branch details.</summary>
    public void Update(string branchName, string bsbCode, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(branchName))
            throw new ArgumentException("Branch name cannot be empty.", nameof(branchName));
        if (string.IsNullOrWhiteSpace(bsbCode))
            throw new ArgumentException("BSB code cannot be empty.", nameof(bsbCode));

        BranchName = branchName;
        BsbCode = bsbCode;
        IsActive = isActive;
    }
}
