using System;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.SDK.Interfaces;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.Infrastructure.Services;

/// <summary>
/// Infrastructure notification service that routes alerts to application logger diagnostics.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationService"/> class.
    /// </summary>
    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public Task SendAsync(string channel, string recipient, string subject, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending notification via Channel: {Channel} to {Recipient}\nSubject: {Subject}\nBody: {Message}",
            channel, recipient, subject, message);

        return Task.CompletedTask;
    }
}
