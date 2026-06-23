using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Users.Commands;

// ── Toggle User Active Status ─────────────────────────────────────────────────

/// <summary>
/// Activates or deactivates a user account.
/// Only the system admin cannot be deactivated.
/// </summary>
public sealed record ToggleUserStatusCommand(int UserId, bool Activate) : IRequest<Result<Unit>>;

public sealed class ToggleUserStatusCommandHandler
    : IRequestHandler<ToggleUserStatusCommand, Result<Unit>>
{
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork     _unitOfWork;

    public ToggleUserStatusCommandHandler(IUserRepository userRepo, IUnitOfWork unitOfWork)
    {
        _userRepo   = userRepo   ?? throw new ArgumentNullException(nameof(userRepo));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<Unit>> Handle(ToggleUserStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepo.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
                return Result<Unit>.Failure($"User {request.UserId} not found.");

            if (!request.Activate && user.IsSystemAdmin)
                return Result<Unit>.Failure("The system admin account cannot be deactivated.");

            if (request.Activate)
                user.Activate();
            else
                user.Deactivate();

            _userRepo.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Status toggle failed: {ex.Message}");
        }
    }
}

// ── Reset Password ────────────────────────────────────────────────────────────

/// <summary>
/// Resets a user's password to a provided new hash and flags MustChangePassword.
/// The caller is responsible for hashing the plain-text password before sending the command.
/// </summary>
public sealed record ResetUserPasswordCommand(int UserId, string NewPasswordHash) : IRequest<Result<Unit>>;

public sealed class ResetUserPasswordCommandHandler
    : IRequestHandler<ResetUserPasswordCommand, Result<Unit>>
{
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork     _unitOfWork;

    public ResetUserPasswordCommandHandler(IUserRepository userRepo, IUnitOfWork unitOfWork)
    {
        _userRepo   = userRepo   ?? throw new ArgumentNullException(nameof(userRepo));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<Unit>> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepo.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
                return Result<Unit>.Failure($"User {request.UserId} not found.");

            user.UpdatePassword(request.NewPasswordHash);
            user.ForcePasswordChange();

            _userRepo.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Password reset failed: {ex.Message}");
        }
    }
}

// ── Create New User ───────────────────────────────────────────────────────────

/// <summary>
/// Creates a new user account for the specified company.
/// </summary>
public sealed record CreateUserCommand(
    int    CompanyId,
    string Username,
    string DisplayName,
    string PasswordHash,
    bool   IsSystemAdmin = false) : IRequest<Result<int>>;

public sealed class CreateUserCommandHandler
    : IRequestHandler<CreateUserCommand, Result<int>>
{
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork     _unitOfWork;

    public CreateUserCommandHandler(IUserRepository userRepo, IUnitOfWork unitOfWork)
    {
        _userRepo   = userRepo   ?? throw new ArgumentNullException(nameof(userRepo));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<int>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _userRepo.GetByUsernameAsync(request.Username, request.CompanyId, cancellationToken);
            if (existing != null)
                return Result<int>.Failure($"Username '{request.Username}' is already taken.");

            var user = Domain.Entities.Company.UserAccount.Create(
                companyId:         request.CompanyId,
                username:          request.Username,
                passwordHash:      request.PasswordHash,
                displayName:       request.DisplayName,
                isSystemAdmin:     request.IsSystemAdmin,
                mustChangePassword: true);

            await _userRepo.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<int>.Success(user.Id);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure($"User creation failed: {ex.Message}");
        }
    }
}
