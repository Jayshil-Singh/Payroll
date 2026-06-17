using System;

namespace FijiPayroll.Domain.Events;

/// <summary>
/// Domain event raised when the first-run onboarding company setup is successfully committed.
/// </summary>
public sealed class CompanySetupCompletedEvent : IDomainEvent
{
    /// <summary>Gets the initialized company tenant ID.</summary>
    public int CompanyId { get; }

    /// <summary>Gets the unique execution request identifier.</summary>
    public Guid ExecutionId { get; }

    /// <summary>Gets the username of the administrator who completed the setup.</summary>
    public string CompletedBy { get; }

    /// <inheritdoc />
    public DateTime OccurredOn { get; }

    /// <summary>Initializes the event.</summary>
    public CompanySetupCompletedEvent(int companyId, Guid executionId, string completedBy)
    {
        if (companyId <= 0)
            throw new ArgumentException("Company ID must be positive.", nameof(companyId));
        if (executionId == Guid.Empty)
            throw new ArgumentException("Execution ID cannot be empty.", nameof(executionId));
        if (string.IsNullOrWhiteSpace(completedBy))
            throw new ArgumentException("Completed by username is required.", nameof(completedBy));

        CompanyId = companyId;
        ExecutionId = executionId;
        CompletedBy = completedBy;
        OccurredOn = DateTime.UtcNow;
    }
}
