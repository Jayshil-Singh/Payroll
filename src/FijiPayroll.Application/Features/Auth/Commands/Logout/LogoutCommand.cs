using FijiPayroll.Application.Common.Models;
using MediatR;

namespace FijiPayroll.Application.Features.Auth.Commands.Logout;

/// <summary>
/// CQRS Command request to log out the current user and clear their authenticated session.
/// </summary>
public sealed record LogoutCommand : IRequest<Result<Unit>>;
