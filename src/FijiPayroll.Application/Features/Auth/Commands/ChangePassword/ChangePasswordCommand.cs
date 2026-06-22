using FijiPayroll.Application.Common.Models;
using MediatR;

namespace FijiPayroll.Application.Features.Auth.Commands.ChangePassword;

/// <summary>
/// CQRS Command request to change the password of the currently authenticated user.
/// </summary>
public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword
) : IRequest<Result<Unit>>;
