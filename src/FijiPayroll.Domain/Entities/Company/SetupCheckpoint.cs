using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using System;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain entity representing a milestone checkpoint completed during setup wizard execution runs.
/// </summary>
public sealed class SetupCheckpoint : SoftDeleteEntity
{
    private SetupCheckpoint() { }

    /// <summary>Gets the company tenant ID.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the unique execution request identifier.</summary>
    public Guid ExecutionId { get; private set; }

    /// <summary>Gets the setup wizard step related to this checkpoint.</summary>
    public WizardStep Step { get; private set; }

    /// <summary>Gets the timestamp when this checkpoint was started.</summary>
    public DateTime StartedUtc { get; private set; }

    /// <summary>Gets the timestamp when this checkpoint was completed.</summary>
    public DateTime? CompletedUtc { get; private set; }

    /// <summary>Gets the validation status state message of the checkpoint.</summary>
    public string Status { get; private set; } = string.Empty;

    /// <summary>Gets description details about the step progress.</summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>Factory method to create a new SetupCheckpoint.</summary>
    public static SetupCheckpoint Create(
        int companyId,
        Guid executionId,
        WizardStep step,
        string status,
        string message)
    {
        if (companyId <= 0)
            throw new ArgumentException("Company ID must be positive.", nameof(companyId));
        if (executionId == Guid.Empty)
            throw new ArgumentException("Execution ID Guid cannot be empty.", nameof(executionId));

        return new SetupCheckpoint
        {
            CompanyId = companyId,
            ExecutionId = executionId,
            Step = step,
            StartedUtc = DateTime.UtcNow,
            Status = status ?? "Pending",
            Message = message ?? string.Empty
        };
    }

    /// <summary>Marks this checkpoint completed.</summary>
    public void MarkCompleted(string status = "Completed", string message = "")
    {
        CompletedUtc = DateTime.UtcNow;
        Status = status;
        if (!string.IsNullOrWhiteSpace(message))
        {
            Message = message;
        }
    }
}
