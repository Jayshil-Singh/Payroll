using System;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.Application.Common.Behaviours;

/// <summary>
/// Pipeline behaviour that enforces role-based security via permission validation.
/// If a request implements <see cref="IRequirePermission"/>, it validates the permissions of the current user.
/// </summary>
public sealed class AuthorizationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuthorizationBehaviour<TRequest, TResponse>> _logger;

    public AuthorizationBehaviour(
        ICurrentUserService currentUserService,
        ILogger<AuthorizationBehaviour<TRequest, TResponse>> logger)
    {
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IRequirePermission permissionRequest)
        {
            var requiredPermission = permissionRequest.RequiredPermission;

            _logger.LogInformation("Enforcing permission check for request {RequestName}. Required: {Permission}",
                typeof(TRequest).Name, requiredPermission);

            if (!_currentUserService.HasPermission(requiredPermission))
            {
                _logger.LogWarning("Access Denied for user '{User}'. Lacks permission: '{Permission}' for request {RequestName}",
                    _currentUserService.Username, requiredPermission, typeof(TRequest).Name);
                
                throw new ForbiddenAccessException(requiredPermission);
            }
        }

        return await next();
    }
}
