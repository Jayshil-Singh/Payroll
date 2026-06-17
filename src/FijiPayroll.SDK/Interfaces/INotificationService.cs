using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.SDK.Interfaces;

/// <summary>
/// Interface defining alert transmission methods across notification channels (Email, Teams, Slack, Webhooks, etc.).
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Dispatches an alert asynchronously.
    /// </summary>
    /// <param name="channel">The target channel (e.g., "Email", "SMS", "Teams").</param>
    /// <param name="recipient">The recipient address, phone number, or webhook URL.</param>
    /// <param name="subject">The notification subject header.</param>
    /// <param name="message">The notification body content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(
        string channel,
        string recipient,
        string subject,
        string message,
        CancellationToken cancellationToken = default);
}
