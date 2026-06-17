using FijiPayroll.Domain.Entities.Common;
using System;
using System.Collections.Generic;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain entity representing a bank master configuration.
/// </summary>
public sealed class BankMaster : SoftDeleteEntity
{
    private readonly List<BankBranch> _branches = new();

    private BankMaster() { }

    /// <summary>Gets the company tenant ID.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the unique bank code (e.g., BSP, ANZ).</summary>
    public string BankCode { get; private set; } = string.Empty;

    /// <summary>Gets the full name of the bank.</summary>
    public string BankName { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether this bank is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets the collection of branches associated with this bank.</summary>
    public IReadOnlyCollection<BankBranch> Branches => _branches.AsReadOnly();

    /// <summary>Factory method to create a new BankMaster.</summary>
    public static BankMaster Create(
        int companyId,
        string bankCode,
        string bankName,
        bool isActive = true)
    {
        if (companyId <= 0)
            throw new ArgumentException("Company ID must be positive.", nameof(companyId));
        if (string.IsNullOrWhiteSpace(bankCode))
            throw new ArgumentException("Bank code cannot be empty.", nameof(bankCode));
        if (string.IsNullOrWhiteSpace(bankName))
            throw new ArgumentException("Bank name cannot be empty.", nameof(bankName));

        return new BankMaster
        {
            CompanyId = companyId,
            BankCode = bankCode,
            BankName = bankName,
            IsActive = isActive
        };
    }

    /// <summary>Updates the bank master details.</summary>
    public void Update(string bankName, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(bankName))
            throw new ArgumentException("Bank name cannot be empty.", nameof(bankName));

        BankName = bankName;
        IsActive = isActive;
    }

    /// <summary>Adds a branch to this bank.</summary>
    public void AddBranch(BankBranch branch)
    {
        ArgumentNullException.ThrowIfNull(branch);
        if (branch.BankMasterId != Id && Id != 0)
            throw new InvalidOperationException("Branch does not belong to this bank.");

        _branches.Add(branch);
    }
}
