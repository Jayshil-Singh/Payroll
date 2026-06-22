using System;

namespace FijiPayroll.Domain.Events;

/// <summary>
/// Domain event raised during calculation when an audit warning, carry-forward,
/// or partial deduction flag occurs.
/// </summary>
public sealed class PayrollAuditEvent : IDomainEvent
{
    /// <summary>
    /// Gets the unique audit event code (e.g. INSUFFICIENT_NET_PAY_FOR_VOLUNTARY_DEDUCTIONS).
    /// </summary>
    public string EventCode { get; }

    /// <summary>
    /// Gets the severity level of the event (e.g. "Warning", "Error", "Audit").
    /// </summary>
    public string Severity { get; }

    /// <summary>
    /// Gets the detailed message describing the event context.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the name of the affected employee.
    /// </summary>
    public string AffectedEmployee { get; }

    /// <inheritdoc />
    public DateTime OccurredOn { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PayrollAuditEvent"/> class.
    /// </summary>
    public PayrollAuditEvent(string eventCode, string severity, string message, string affectedEmployee)
    {
        EventCode = eventCode ?? throw new ArgumentNullException(nameof(eventCode));
        Severity = severity ?? throw new ArgumentNullException(nameof(severity));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        AffectedEmployee = affectedEmployee ?? string.Empty;
        OccurredOn = DateTime.UtcNow;
    }
}
