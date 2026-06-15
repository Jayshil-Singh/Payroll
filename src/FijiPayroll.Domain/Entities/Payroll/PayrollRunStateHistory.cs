using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Shared.Guards;
using System;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Audit record tracking state machine transitions for compliance audits.
/// </summary>
public sealed class PayrollRunStateHistory : BaseEntity
{
    private string _changedBy = string.Empty;
    private string _notes = string.Empty;

    private PayrollRunStateHistory() { }

    /// <summary>
    /// Foreign key to PayrollRun.
    /// </summary>
    public int PayrollRunId { get; private set; }

    /// <summary>
    /// Source status before the transition.
    /// </summary>
    public PayrollRunStatus FromStatus { get; private set; }

    /// <summary>
    /// Destination status after the transition.
    /// </summary>
    public PayrollRunStatus ToStatus { get; private set; }

    /// <summary>
    /// Who triggered the state transition.
    /// </summary>
    public string ChangedBy
    {
        get => _changedBy;
        private set => _changedBy = Guard.AgainstNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Timestamp when the change occurred.
    /// </summary>
    public DateTime ChangedAt { get; private set; }

    /// <summary>
    /// Compliance audit note explaining the transition.
    /// </summary>
    public string Notes
    {
        get => _notes;
        private set => _notes = value ?? string.Empty;
    }

    /// <summary>
    /// Factory method to create a status transition log.
    /// </summary>
    public static PayrollRunStateHistory Create(
        int payrollRunId,
        PayrollRunStatus fromStatus,
        PayrollRunStatus toStatus,
        string changedBy,
        string notes)
    {
        return new PayrollRunStateHistory
        {
            PayrollRunId = payrollRunId,
            FromStatus = Guard.AgainstInvalidEnum(fromStatus),
            ToStatus = Guard.AgainstInvalidEnum(toStatus),
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow,
            Notes = notes
        };
    }
}
