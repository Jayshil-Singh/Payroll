using System;

namespace FijiPayroll.Domain.Events;

/// <summary>
/// Marker interface for all domain events.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Gets the UTC timestamp when the event occurred.</summary>
    DateTime OccurredOn { get; }
}
