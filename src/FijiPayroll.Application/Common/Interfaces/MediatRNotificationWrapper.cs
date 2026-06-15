using MediatR;

namespace FijiPayroll.Application.Common.Interfaces;

/// <summary>
/// Wrapper to allow any plain domain event object to be published as a MediatR INotification.
/// </summary>
/// <typeparam name="T">The domain event type.</typeparam>
public sealed class MediatRNotificationWrapper<T> : INotification
{
    /// <summary>Gets the underlying domain event.</summary>
    public T Event { get; }

    /// <summary>Initializes the wrapper.</summary>
    public MediatRNotificationWrapper(T @event)
    {
        Event = @event;
    }
}
