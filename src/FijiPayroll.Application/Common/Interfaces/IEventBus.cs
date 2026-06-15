using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Common.Interfaces;

/// <summary>
/// Abstraction for publishing and subscribing to in-memory domain events.
/// Decouples event dispatching from operational transactions.
/// </summary>
public interface IEventBus
{
    /// <summary>Publishes an event asynchronously to all registered handlers.</summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;

    /// <summary>Registers an event listener action.</summary>
    void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;
}
