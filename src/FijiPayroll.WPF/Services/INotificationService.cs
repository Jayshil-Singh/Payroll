using System;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// Type of toast notification to show.
/// </summary>
public enum NotificationType
{
    Success,
    Error,
    Warning,
    Info
}

/// <summary>
/// Model representing an active notification toast message.
/// </summary>
public sealed class NotificationMessage
{
    public string Title { get; }
    public string Message { get; }
    public NotificationType Type { get; }
    public DateTime Timestamp { get; }

    public NotificationMessage(string title, string message, NotificationType type)
    {
        Title = title;
        Message = message;
        Type = type;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Service coordinating popups, alerts, and toasts inside the WPF desktop application window.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Event fired when a new notification is requested to be displayed.
    /// </summary>
    event Action<NotificationMessage>? NotificationRaised;

    /// <summary>
    /// Dispatches a notification to be displayed to the user.
    /// </summary>
    void Show(string title, string message, NotificationType type = NotificationType.Info);

    /// <summary>
    /// Dispatches a success notification.
    /// </summary>
    void Success(string message, string title = "Success");

    /// <summary>
    /// Dispatches an error notification.
    /// </summary>
    void Error(string message, string title = "Error");

    /// <summary>
    /// Dispatches a warning notification.
    /// </summary>
    void Warning(string message, string title = "Warning");

    /// <summary>
    /// Dispatches an info notification.
    /// </summary>
    void Info(string message, string title = "Information");
}
