using FijiPayroll.Application.Common.Models;
using MediatR;

namespace FijiPayroll.Application.Features.Auth.Commands.Login;

/// <summary>
/// CQRS Command request to authenticate a user context.
/// </summary>
public sealed record LoginCommand(
    string Username,
    string Password,
    int CompanyId
) : IRequest<Result<AuthenticatedSession>>;
