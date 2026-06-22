using System;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.Application.Features.Auth.Commands.Login;

/// <summary>
/// Handler for executing user authentication and establishing active sessions.
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthenticatedSession>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<AuthenticatedSession>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing login attempt for user '{Username}' in company ID {CompanyId}", request.Username, request.CompanyId);

        // 1. Fetch user account
        var user = await _userRepository.GetByUsernameAsync(request.Username, request.CompanyId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Authentication failed: User '{Username}' not found in company ID {CompanyId}", request.Username, request.CompanyId);
            return Result<AuthenticatedSession>.Failure("Invalid username or password.");
        }

        // 2. Lockout checks
        if (user.IsLockedOut())
        {
            _logger.LogWarning("Authentication rejected: User '{Username}' account is currently locked out until {LockedUntil} UTC", request.Username, user.LockedUntil);
            return Result<AuthenticatedSession>.Failure($"Account is locked due to multiple failed login attempts. Please try again after {user.LockedUntil?.ToLocalTime():yyyy-MM-dd HH:mm:ss}.");
        }

        // 3. Activation check
        if (!user.IsActive)
        {
            _logger.LogWarning("Authentication rejected: User '{Username}' account is deactivated", request.Username);
            return Result<AuthenticatedSession>.Failure("Your account has been deactivated. Please contact your system administrator.");
        }

        // 4. Validate password
        bool isPasswordValid = _passwordHasher.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Authentication failed: Incorrect password for user '{Username}'", request.Username);
            
            user.RecordFailedLogin();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return Result<AuthenticatedSession>.Failure("Invalid username or password.");
        }

        // 5. Successful login, reset lockout
        user.RecordSuccessfulLogin();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Resolve permissions and roles
        var permissions = await _userRepository.GetPermissionsForUserAsync(user.Id, cancellationToken);
        var roles = new List<string>();
        foreach (var userRole in user.Roles)
        {
            roles.Add(userRole.RoleName);
        }

        // 7. Establish session
        var session = new AuthenticatedSession
        {
            UserId = user.Id,
            Username = user.Username,
            CompanyIds = new List<int> { user.CompanyId },
            Roles = roles,
            Permissions = permissions,
            MustChangePassword = user.MustChangePassword
        };

        _logger.LogInformation("User '{Username}' authenticated successfully. Session established.", user.Username);

        return Result<AuthenticatedSession>.Success(session);
    }
}
