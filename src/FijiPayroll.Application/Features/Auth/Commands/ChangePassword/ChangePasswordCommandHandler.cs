using System;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Exceptions;
using FijiPayroll.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.Application.Features.Auth.Commands.ChangePassword;

/// <summary>
/// Handler for changing the password of the current user.
/// </summary>
public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result<Unit>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Unit>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        int userId = _currentUserService.UserId;
        _logger.LogInformation("Processing change password request for user ID {UserId}", userId);

        if (userId <= 0)
        {
            _logger.LogWarning("Change password rejected: User is not authenticated.");
            return Result<Unit>.Failure("User is not authenticated.");
        }

        // 1. Fetch user account
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Change password failed: User with ID {UserId} not found.", userId);
            return Result<Unit>.Failure("User account not found.");
        }

        // 2. Validate current password
        bool isCurrentValid = _passwordHasher.Verify(request.CurrentPassword, user.PasswordHash);
        if (!isCurrentValid)
        {
            _logger.LogWarning("Change password failed: Incorrect current password for user '{Username}'", user.Username);
            return Result<Unit>.Failure("Incorrect current password.");
        }

        // 3. Validate new password against policy
        try
        {
            PasswordPolicy.Validate(request.NewPassword);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Change password failed: Password policy violation for user '{Username}': {Error}", user.Username, ex.Message);
            return Result<Unit>.Failure(ex.Message);
        }

        // 4. Update password
        string newHash = _passwordHasher.Hash(request.NewPassword);
        user.UpdatePassword(newHash);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Password changed successfully for user '{Username}'.", user.Username);

        return Result<Unit>.Success(Unit.Value);
    }
}
