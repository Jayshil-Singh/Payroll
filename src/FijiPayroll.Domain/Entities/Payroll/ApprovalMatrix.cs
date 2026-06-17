using System;
using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Domain model mapping role boundaries and verification thresholds for manual calculations override approvals.
/// </summary>
public sealed class ApprovalMatrix : AuditableEntity
{
    /// <summary>Gets the multi-tenant company identifier.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the targeted role group required to sign off.</summary>
    public ApprovalRole Role { get; private set; }

    /// <summary>Gets the classified system action or code checked (e.g. "FRCS_Override", "FNPF_Filing").</summary>
    public string ActionType { get; private set; } = string.Empty;

    /// <summary>Gets the minimum value required to trigger this matrix rule.</summary>
    public decimal MinThreshold { get; private set; }

    /// <summary>Gets the maximum value this role is authorized to override.</summary>
    public decimal MaxThreshold { get; private set; }

    /// <summary>Gets a value indicating whether this matrix configuration rule is active.</summary>
    public bool IsActive { get; private set; }

    private ApprovalMatrix() { } // For EF Core

    /// <summary>
    /// Factory method to construct a new ApprovalMatrix.
    /// </summary>
    public static ApprovalMatrix Create(
        int companyId,
        ApprovalRole role,
        string actionType,
        decimal minThreshold,
        decimal maxThreshold)
    {
        if (companyId <= 0) throw new ArgumentOutOfRangeException(nameof(companyId));
        if (string.IsNullOrWhiteSpace(actionType)) throw new ArgumentException("Action type cannot be empty.", nameof(actionType));
        if (minThreshold > maxThreshold) throw new ArgumentException("Min threshold cannot exceed max threshold.");

        return new ApprovalMatrix
        {
            CompanyId = companyId,
            Role = role,
            ActionType = actionType,
            MinThreshold = minThreshold,
            MaxThreshold = maxThreshold,
            IsActive = true
        };
    }

    /// <summary>Deactivates the approval matrix config.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Activates the approval matrix config.</summary>
    public void Activate() => IsActive = true;
}
