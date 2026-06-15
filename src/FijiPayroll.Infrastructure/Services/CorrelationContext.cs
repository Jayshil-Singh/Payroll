using FijiPayroll.Application.Common.Interfaces;
using System;

namespace FijiPayroll.Infrastructure.Services;

/// <summary>
/// Scoped implementation of ICorrelationContext generating a unique correlation ID per scope.
/// </summary>
public sealed class CorrelationContext : ICorrelationContext
{
    /// <inheritdoc />
    public Guid CorrelationId { get; } = Guid.NewGuid();
}
