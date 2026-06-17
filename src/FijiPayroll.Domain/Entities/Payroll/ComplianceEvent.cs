using System;
using FijiPayroll.Domain.Entities.Common;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Domain model representing a structured audit event logged by the compliance engine.
/// Captures transition details, machine identities, and application version info.
/// </summary>
public sealed class ComplianceEvent : BaseEntity
{
    /// <summary>Gets the unique operation correlation identifier.</summary>
    public Guid CorrelationId { get; private set; }

    /// <summary>Gets the multi-tenant company identifier.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the structured event action classification.</summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>Gets the user who initiated the event.</summary>
    public string User { get; private set; } = string.Empty;

    /// <summary>Gets the machine/host details where the execution took place.</summary>
    public string Machine { get; private set; } = string.Empty;

    /// <summary>Gets the running platform compilation version.</summary>
    public string ApplicationVersion { get; private set; } = string.Empty;

    /// <summary>Gets the raw JSON payload describing context parameters.</summary>
    public string PayloadJson { get; private set; } = string.Empty;

    /// <summary>Gets the UTC event log time.</summary>
    public DateTime CreatedAt { get; private set; }

    private ComplianceEvent() { } // For EF Core

    /// <summary>
    /// Factory method to construct a new ComplianceEvent.
    /// </summary>
    public static ComplianceEvent Create(
        Guid correlationId,
        int companyId,
        string eventType,
        string user,
        string machine,
        string applicationVersion,
        string payloadJson)
    {
        if (correlationId == Guid.Empty) throw new ArgumentException("Correlation ID cannot be empty.", nameof(correlationId));
        if (string.IsNullOrWhiteSpace(eventType)) throw new ArgumentException("Event type cannot be empty.", nameof(eventType));
        if (string.IsNullOrWhiteSpace(user)) throw new ArgumentException("User name cannot be empty.", nameof(user));

        return new ComplianceEvent
        {
            CorrelationId = correlationId,
            CompanyId = companyId,
            EventType = eventType,
            User = user,
            Machine = machine,
            ApplicationVersion = applicationVersion,
            PayloadJson = payloadJson,
            CreatedAt = DateTime.UtcNow
        };
    }
}
