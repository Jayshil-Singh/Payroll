using FluentValidation;
using FijiPayroll.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that runs all registered <see cref="IValidator{T}"/>
/// instances for the incoming request before the handler executes.
///
/// Returns <see cref="Result.Failure(IReadOnlyList{string})"/> when validation fails
/// rather than throwing, keeping the Result pattern clean across layer boundaries.
/// </summary>
/// <typeparam name="TRequest">The MediatR request type.</typeparam>
/// <typeparam name="TResponse">The handler response type (must be <see cref="Result"/> or subclass).</typeparam>
public sealed class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest  : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehaviour<TRequest, TResponse>> _logger;

    /// <summary>Initialises the behaviour with all registered validators for the request.</summary>
    public ValidationBehaviour(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehaviour<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger     = logger;
    }

    /// <inheritdoc/>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        _logger.LogDebug(
            "Running {ValidatorCount} validators for {RequestType}",
            _validators.Count(),
            typeof(TRequest).Name);

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => f.ErrorMessage)
            .ToList();

        if (failures.Count > 0)
        {
            _logger.LogWarning(
                "Validation failed for {RequestType}: {Errors}",
                typeof(TRequest).Name,
                string.Join(" | ", failures));

            // Use reflection to call the static Failure factory on the concrete Result type
            var failureMethod = typeof(TResponse)
                .GetMethod(nameof(Result.Failure), [typeof(IReadOnlyList<string>)])
                ?? typeof(Result)
                   .GetMethod(nameof(Result.Failure), [typeof(IReadOnlyList<string>)]);

            return (TResponse)failureMethod!.Invoke(null, [(IReadOnlyList<string>)failures])!;
        }

        return await next();
    }
}
