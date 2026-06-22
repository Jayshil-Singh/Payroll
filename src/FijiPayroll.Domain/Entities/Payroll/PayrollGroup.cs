using FijiPayroll.Domain.Entities.Common;
using System;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Domain entity representing a Payroll Group for targeted processing and default parameters.
/// </summary>
public sealed class PayrollGroup : AuditableEntity
{
    public int CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? FilterCriteria { get; private set; } // JSON representing filter settings
    public int? DefaultBankAccountId { get; private set; }
    public int? DefaultCalendarId { get; private set; }
    public string? DefaultCostCentre { get; private set; }
    public int? DefaultLeaveRulesPackageId { get; private set; }
    public int? ApprovalWorkflowId { get; private set; }

    private PayrollGroup() { } // For EF Core

    public static PayrollGroup Create(
        int companyId,
        string name,
        string code,
        string? filterCriteria,
        int? defaultBankAccountId,
        int? defaultCalendarId,
        string? defaultCostCentre,
        int? defaultLeaveRulesPackageId,
        int? approvalWorkflowId,
        string createdBy)
    {
        if (companyId <= 0) throw new ArgumentOutOfRangeException(nameof(companyId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Group name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Group code cannot be empty.", nameof(code));

        return new PayrollGroup
        {
            CompanyId = companyId,
            Name = name,
            Code = code,
            FilterCriteria = filterCriteria,
            DefaultBankAccountId = defaultBankAccountId,
            DefaultCalendarId = defaultCalendarId,
            DefaultCostCentre = defaultCostCentre,
            DefaultLeaveRulesPackageId = defaultLeaveRulesPackageId,
            ApprovalWorkflowId = approvalWorkflowId,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }
}
