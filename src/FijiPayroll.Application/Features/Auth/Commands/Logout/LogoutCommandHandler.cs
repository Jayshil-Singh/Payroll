using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.Application.Features.Auth.Commands.Logout;

/// <summary>
/// Handler for executing user logout.
/// </summary>
public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result<Unit>>
{
    private readonly IAuthSessionStore _sessionStore;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(IAuthSessionStore sessionStore, ILogger<LogoutCommandHandler> logger)
    {
        _sessionStore = sessionStore;
        _logger = logger;
    }

    public Task<Result<Unit>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing user logout request.");
        _sessionStore.Clear();
        return Task.FromResult(Result<Unit>.Success(Unit.Value));
    }
}
