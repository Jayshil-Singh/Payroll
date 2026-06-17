using System;
using FijiPayroll.Domain.Entities.Common;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Domain model linking submission histories together to form a clear chain of amendments.
/// </summary>
public sealed class ComplianceAmendment : BaseEntity
{
    /// <summary>Gets the original baseline submission ID in the chain.</summary>
    public int OriginalSubmissionId { get; private set; }

    /// <summary>Gets the immediate previous submission ID being corrected.</summary>
    public int PreviousSubmissionId { get; private set; }

    /// <summary>Gets the new replacement submission ID representing the current state.</summary>
    public int CurrentSubmissionId { get; private set; }

    /// <summary>Gets the detailed explanation explaining why the amendment was filed.</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>Gets the user who authorized the amendment.</summary>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <summary>Gets the timestamp when the amendment log was filed.</summary>
    public DateTime CreatedUtc { get; private set; }

    private ComplianceAmendment() { } // For EF Core

    /// <summary>
    /// Factory method to construct a new ComplianceAmendment.
    /// </summary>
    public static ComplianceAmendment Create(
        int originalSubmissionId,
        int previousSubmissionId,
        int currentSubmissionId,
        string reason,
        string createdBy)
    {
        if (originalSubmissionId <= 0) throw new ArgumentOutOfRangeException(nameof(originalSubmissionId));
        if (previousSubmissionId <= 0) throw new ArgumentOutOfRangeException(nameof(previousSubmissionId));
        if (currentSubmissionId <= 0) throw new ArgumentOutOfRangeException(nameof(currentSubmissionId));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason description cannot be empty.", nameof(reason));

        return new ComplianceAmendment
        {
            OriginalSubmissionId = originalSubmissionId,
            PreviousSubmissionId = previousSubmissionId,
            CurrentSubmissionId = currentSubmissionId,
            Reason = reason,
            CreatedBy = createdBy,
            CreatedUtc = DateTime.UtcNow
        };
    }
}
