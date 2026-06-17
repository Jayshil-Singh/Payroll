using System;
using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Domain model representing a persistent notification message in the outbound queue.
/// </summary>
public sealed class Notification : BaseEntity
{
    /// <summary>Gets the multi-tenant company identifier.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the targeted communication channel.</summary>
    public NotificationChannel Channel { get; private set; }

    /// <summary>Gets the recipient address, phone number, or webhook URL.</summary>
    public string Recipient { get; private set; } = string.Empty;

    /// <summary>Gets the subject header of the notification.</summary>
    public string Subject { get; private set; } = string.Empty;

    /// <summary>Gets the main body message text.</summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>Gets the current delivery status ("Pending", "Sent", "Failed").</summary>
    public string Status { get; private set; } = "Pending";

    /// <summary>Gets the number of times sending has been attempted.</summary>
    public int RetryCount { get; private set; }

    /// <summary>Gets the failure log message if delivery failed.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Gets the creation time.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Gets the timestamp when the notification was successfully sent.</summary>
    public DateTime? SentAt { get; private set; }

    private Notification() { } // For EF Core

    /// <summary>
    /// Factory method to construct a new Notification.
    /// </summary>
    public static Notification Create(
        int companyId,
        NotificationChannel channel,
        string recipient,
        string subject,
        string message)
    {
        if (string.IsNullOrWhiteSpace(recipient)) throw new ArgumentException("Recipient address cannot be empty.", nameof(recipient));
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject header cannot be empty.", nameof(subject));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message body cannot be empty.", nameof(message));

        return new Notification
        {
            CompanyId = companyId,
            Channel = channel,
            Recipient = recipient,
            Subject = subject,
            Message = message,
            Status = "Pending",
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Marks the notification as successfully sent.</summary>
    public void MarkAsSent()
    {
        Status = "Sent";
        SentAt = DateTime.UtcNow;
        ErrorMessage = null;
    }

    /// <summary>Marks the notification as failed, incrementing retry tracking.</summary>
    public void MarkAsFailed(string errorMessage)
    {
        RetryCount++;
        ErrorMessage = errorMessage;
        Status = "Failed";
    }

    /// <summary>Resets status to pending to schedule another retry.</summary>
    public void ResetForRetry()
    {
        Status = "Pending";
        ErrorMessage = null;
    }
}
