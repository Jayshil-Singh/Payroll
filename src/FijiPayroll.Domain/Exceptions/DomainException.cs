namespace FijiPayroll.Domain.Exceptions;

/// <summary>
/// Base exception for all domain rule violations within the Fiji Payroll domain layer.
/// Thrown when business invariants are broken (not for infrastructure or validation errors).
/// </summary>
public class DomainException : Exception
{
    /// <inheritdoc/>
    public DomainException(string message) : base(message) { }

    /// <inheritdoc/>
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}
