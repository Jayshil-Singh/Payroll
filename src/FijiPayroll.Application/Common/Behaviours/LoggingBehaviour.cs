using FijiPayroll.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FijiPayroll.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that logs the start, completion, and elapsed time
/// of every command and query. Uses structured logging via Serilog-compatible
/// <see cref="ILogger{T}"/>.
///
/// Logs a warning when any handler takes longer than <see cref="WarningThresholdMs"/> milliseconds.
/// </summary>
/// <typeparam name="TRequest">The MediatR request type.</typeparam>
/// <typeparam name="TResponse">The handler response type.</typeparam>
public sealed class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Handlers slower than this threshold trigger a Warning-level log.</summary>
    private const int WarningThresholdMs = 500;

    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    /// <summary>Initialises the behaviour with a logger.</summary>
    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("→ Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > WarningThresholdMs)
            {
                _logger.LogWarning(
                    "⚠ Slow request: {RequestName} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
                    requestName, stopwatch.ElapsedMilliseconds, WarningThresholdMs);
            }
            else
            {
                _logger.LogInformation(
                    "✓ Handled {RequestName} in {ElapsedMs}ms",
                    requestName, stopwatch.ElapsedMilliseconds);
            }

            // Log Result failures at warning level
            if (response is Result result && result.IsFailure)
            {
                _logger.LogWarning(
                    "✗ {RequestName} returned failure: {Errors}",
                    requestName, string.Join(" | ", result.Errors));
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "✗ {RequestName} threw an unhandled exception after {ElapsedMs}ms",
                requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
