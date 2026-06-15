using System;

namespace FijiPayroll.Application.Common.Interfaces;

/// <summary>
/// Holds the transaction correlation tracking ID for the current scope.
/// </summary>
public interface ICorrelationContext
{
    /// <summary>Gets the correlation ID of the current scope.</summary>
    Guid CorrelationId { get; }
}
