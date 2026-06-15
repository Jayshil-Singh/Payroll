using System;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// Service coordinating the raising of notification toast events to UI overlays.
/// </summary>
public sealed class NotificationService : INotificationService
{
    /// <inheritdoc />
    public event Action<NotificationMessage>? NotificationRaised;

    /// <inheritdoc />
    public void Show(string title, string message, NotificationType type = NotificationType.Info)
    {
        NotificationRaised?.Invoke(new NotificationMessage(title, message, type));
    }

    /// <inheritdoc />
    public void Success(string message, string title = "Success")
    {
        Show(title, message, NotificationType.Success);
    }

    /// <inheritdoc />
    public void Error(string message, string title = "Error")
    {
        Show(title, message, NotificationType.Error);
    }

    /// <inheritdoc />
    public void Warning(string message, string title = "Warning")
    {
        Show(title, message, NotificationType.Warning);
    }

    /// <inheritdoc />
    public void Info(string message, string title = "Information")
    {
        Show(title, message, NotificationType.Info);
    }
}
