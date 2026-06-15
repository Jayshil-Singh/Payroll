using MediatR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Common.Interfaces;

namespace FijiPayroll.Infrastructure.Services;

/// <summary>
/// Thread-safe, in-memory implementation of IEventBus.
/// Combines MediatR notification dispatching with dynamic runtime subscriber support.
/// </summary>
public sealed class EventBus : IEventBus
{
    private readonly IMediator _mediator;
    private readonly ConcurrentDictionary<Type, List<Func<object, Task>>> _subscribers = new();

    /// <summary>Initializes the event bus with MediatR.</summary>
    public EventBus(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        // 1. Dispatch via MediatR pipeline to all compiled notification handlers
        var wrapper = new MediatRNotificationWrapper<TEvent>(@event);
        await _mediator.Publish(wrapper, cancellationToken).ConfigureAwait(false);

        // 2. Dispatch to dynamic programmatic subscribers in the background
        if (_subscribers.TryGetValue(typeof(TEvent), out var handlers))
        {
            List<Func<object, Task>> handlersCopy;
            lock (handlers)
            {
                handlersCopy = new List<Func<object, Task>>(handlers);
            }

            foreach (var handler in handlersCopy)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await handler(@event).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Prevent individual subscriber failure from crashing the dispatch pipeline
                    }
                }, cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
    {
        var list = _subscribers.GetOrAdd(typeof(TEvent), _ => new List<Func<object, Task>>());
        lock (list)
        {
            list.Add(obj => handler((TEvent)obj));
        }
    }
}
