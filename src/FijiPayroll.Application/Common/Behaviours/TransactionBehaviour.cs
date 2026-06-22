using System;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.Application.Common.Behaviours;

/// <summary>
/// Pipeline behaviour that wraps the request execution in a database transaction boundary.
/// Applies to any request implementing <see cref="ITransactional"/>.
/// </summary>
public sealed class TransactionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehaviour<TRequest, TResponse>> _logger;

    public TransactionBehaviour(
        IUnitOfWork unitOfWork,
        ILogger<TransactionBehaviour<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ITransactional)
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Starting database transaction for request {RequestName}", requestName);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();

            _logger.LogInformation("Committing database transaction for request {RequestName}", requestName);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught during transaction for request {RequestName}. Rolling back...", requestName);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
