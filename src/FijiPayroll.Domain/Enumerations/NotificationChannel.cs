namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Active channels supported by the notification delivery dispatch engine.
/// </summary>
public enum NotificationChannel
{
    /// <summary>Dispatch via SMTP email gateway.</summary>
    Email = 1,

    /// <summary>Display on internal WPF system notifications HUD.</summary>
    Desktop = 2,

    /// <summary>Dispatch via SMS telecom gateway.</summary>
    SMS = 3,

    /// <summary>Push via generic Webhook endpoint.</summary>
    Webhook = 4,

    /// <summary>Push alert message to Microsoft Teams channel.</summary>
    Teams = 5,

    /// <summary>Push alert message to Slack channel.</summary>
    Slack = 6
}
