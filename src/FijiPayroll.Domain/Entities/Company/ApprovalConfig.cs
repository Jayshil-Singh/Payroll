using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using System;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain entity representing approval configuration hierarchy mapping roles, levels, and user/employee references.
/// </summary>
public sealed class ApprovalConfig : SoftDeleteEntity
{
    private ApprovalConfig() { }

    /// <summary>Gets the company tenant ID.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the associated application User ID (if identity user).</summary>
    public string? UserId { get; private set; }

    /// <summary>Gets the associated Employee ID (if employee).</summary>
    public int? EmployeeId { get; private set; }

    /// <summary>Gets the sequence approval level (e.g. 1 for first-level, 2 for second-level).</summary>
    public int ApprovalLevel { get; private set; }

    /// <summary>Gets the role required for this approval stage.</summary>
    public ApprovalRole Role { get; private set; }

    /// <summary>Gets a value indicating whether this approval configuration is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Factory method to create a new ApprovalConfig.</summary>
    public static ApprovalConfig Create(
        int companyId,
        string? userId,
        int? employeeId,
        int approvalLevel,
        ApprovalRole role,
        bool isActive = true)
    {
        if (companyId <= 0)
            throw new ArgumentException("Company ID must be positive.", nameof(companyId));
        if (string.IsNullOrWhiteSpace(userId) && !employeeId.HasValue)
            throw new ArgumentException("Either UserId or EmployeeId must be supplied.");
        if (!string.IsNullOrWhiteSpace(userId) && employeeId.HasValue)
            throw new ArgumentException("Cannot supply both UserId and EmployeeId.");
        if (approvalLevel <= 0)
            throw new ArgumentException("Approval level must be greater than zero.", nameof(approvalLevel));

        return new ApprovalConfig
        {
            CompanyId = companyId,
            UserId = userId,
            EmployeeId = employeeId,
            ApprovalLevel = approvalLevel,
            Role = role,
            IsActive = isActive
        };
    }

    /// <summary>Updates the approval configuration.</summary>
    public void Update(
        string? userId,
        int? employeeId,
        int approvalLevel,
        ApprovalRole role,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(userId) && !employeeId.HasValue)
            throw new ArgumentException("Either UserId or EmployeeId must be supplied.");
        if (!string.IsNullOrWhiteSpace(userId) && employeeId.HasValue)
            throw new ArgumentException("Cannot supply both UserId and EmployeeId.");
        if (approvalLevel <= 0)
            throw new ArgumentException("Approval level must be greater than zero.", nameof(approvalLevel));

        UserId = userId;
        EmployeeId = employeeId;
        ApprovalLevel = approvalLevel;
        Role = role;
        IsActive = isActive;
    }
}
