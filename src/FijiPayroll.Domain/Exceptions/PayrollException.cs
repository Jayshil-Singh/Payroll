using System;

namespace FijiPayroll.Domain.Exceptions;

/// <summary>
/// Exception thrown when a critical payroll rule is violated during calculation,
/// such as insufficient net pay when using a blocking policy.
/// </summary>
public class PayrollException : DomainException
{
    /// <summary>
    /// Gets the unique audit event code representing this exception type.
    /// </summary>
    public string EventCode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PayrollException"/> class.
    /// </summary>
    public PayrollException(string eventCode, string message) : base(message)
    {
        EventCode = eventCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PayrollException"/> class with an inner exception.
    /// </summary>
    public PayrollException(string eventCode, string message, Exception innerException) : base(message, innerException)
    {
        EventCode = eventCode;
    }
}
