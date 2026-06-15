using FijiPayroll.Domain.Entities.Common;
using System;

namespace FijiPayroll.Domain.Entities.Audit;

/// <summary>
/// Domain entity representing a system-wide transactional event outbox log.
/// </summary>
public sealed class EntityEvent : BaseEntity
{
    private EntityEvent() { }

    /// <summary>Gets the owner company ID.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the system event type name (e.g. EmployeeCreatedEvent).</summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>Gets the serialized event payload.</summary>
    public string Payload { get; private set; } = string.Empty;

    /// <summary>Gets the UTC timestamp when this event occurred.</summary>
    public DateTime OccurredOn { get; private set; }

    /// <summary>Gets the correlation ID of the logical transaction.</summary>
    public Guid CorrelationId { get; private set; }

    /// <summary>Factory method to create an EntityEvent entry.</summary>
    public static EntityEvent Create(
        int companyId,
        string eventType,
        string payload,
        DateTime occurredOn,
        Guid correlationId)
    {
        return new EntityEvent
        {
            CompanyId = companyId,
            EventType = eventType,
            Payload = payload ?? string.Empty,
            OccurredOn = occurredOn,
            CorrelationId = correlationId
        };
    }
}
