using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FijiPayroll.SDK.Interfaces;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.Platform.Notifications;

/// <summary>
/// A high-performance, asynchronous notification queue engine that dispatches messages to various notification services.
/// </summary>
public sealed class NotificationEngine : IDisposable
{
    private readonly IEnumerable<INotificationService> _notificationServices;
    private readonly ILogger<NotificationEngine> _logger;
    private readonly Channel<NotificationWorkItem> _channel;
    private readonly CancellationTokenSource _cts = new();
    private Task? _processingTask;
    private int _isStarted;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationEngine"/> class.
    /// </summary>
    public NotificationEngine(
        IEnumerable<INotificationService> notificationServices,
        ILogger<NotificationEngine> logger)
    {
        _notificationServices = notificationServices ?? throw new ArgumentNullException(nameof(notificationServices));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Unbounded channel for high-throughput queuing
        _channel = Channel.CreateUnbounded<NotificationWorkItem>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    /// <summary>
    /// Starts the background notification processing loop.
    /// </summary>
    public void Start()
    {
        if (Interlocked.CompareExchange(ref _isStarted, 1, 0) == 0)
        {
            _processingTask = Task.Run(ProcessQueueAsync);
            _logger.LogInformation("Notification engine started.");
        }
    }

    /// <summary>
    /// Stops the background notification processing loop.
    /// </summary>
    public void Stop()
    {
        if (Interlocked.CompareExchange(ref _isStarted, 0, 1) == 1)
        {
            _channel.Writer.Complete();
            _cts.Cancel();
            try
            {
                _processingTask?.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // Expected when canceled
            }
            _logger.LogInformation("Notification engine stopped.");
        }
    }

    /// <summary>
    /// Queues a new notification message.
    /// </summary>
    public async Task QueueNotificationAsync(string channel, string recipient, string subject, string message)
    {
        var item = new NotificationWorkItem(channel, recipient, subject, message, 0);
        if (!_channel.Writer.TryWrite(item))
        {
            await _channel.Writer.WriteAsync(item);
        }
        _logger.LogDebug("Notification queued for recipient: {Recipient} on channel: {Channel}", recipient, channel);
    }

    private async Task ProcessQueueAsync()
    {
        var reader = _channel.Reader;
        while (await reader.WaitToReadAsync(_cts.Token))
        {
            while (reader.TryRead(out var item))
            {
                _ = DispatchWithRetryAsync(item, _cts.Token);
            }
        }
    }

    private async Task DispatchWithRetryAsync(NotificationWorkItem item, CancellationToken cancellationToken)
    {
        const int maxRetries = 5;
        try
        {
            // Find a service matching the channel name (e.g. Email, Slack).
            // If none matched, use the first available or log error.
            var service = _notificationServices.FirstOrDefault(); 
            if (service == null)
            {
                _logger.LogError("No notification service implementations available to send message.");
                return;
            }

            _logger.LogInformation("Dispatching notification to '{Recipient}' via channel '{Channel}'", item.Recipient, item.Channel);
            await service.SendAsync(item.Channel, item.Recipient, item.Subject, item.Message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching notification to '{Recipient}'", item.Recipient);
            if (item.RetryCount < maxRetries)
            {
                int delaySeconds = (int)Math.Pow(2, item.RetryCount); // Exponential backoff: 1, 2, 4, 8, 16
                var retryItem = item with { RetryCount = item.RetryCount + 1 };
                _logger.LogWarning("Retrying notification in {DelaySeconds} seconds (Attempt {Attempt}/{Max})", delaySeconds, retryItem.RetryCount, maxRetries);
                
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await _channel.Writer.WriteAsync(retryItem, cancellationToken);
                    }
                }, cancellationToken);
            }
            else
            {
                _logger.LogCritical("Notification failed after reaching maximum retries of {MaxRetries}. Recipient: {Recipient}", maxRetries, item.Recipient);
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Stop();
        _cts.Dispose();
    }

    private sealed record NotificationWorkItem(
        string Channel,
        string Recipient,
        string Subject,
        string Message,
        int RetryCount
    );
}
