using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Users.Queries;

// ── DTO ───────────────────────────────────────────────────────────────────────

public sealed class UserListItemDto
{
    public int      Id          { get; set; }
    public string   DisplayName { get; set; } = string.Empty;
    public string   Username    { get; set; } = string.Empty;
    public string   PrimaryRole { get; set; } = "No Role";
    public bool     IsActive    { get; set; }
    public bool     IsSystemAdmin { get; set; }
    public bool     MustChangePassword { get; set; }
    public bool     IsLockedOut { get; set; }
    public DateTime CreatedAt   { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns all user accounts belonging to the specified company.
/// </summary>
public sealed record GetUserListQuery(int CompanyId) : IRequest<Result<IReadOnlyList<UserListItemDto>>>;

public sealed class GetUserListQueryHandler
    : IRequestHandler<GetUserListQuery, Result<IReadOnlyList<UserListItemDto>>>
{
    private readonly IUserRepository _userRepo;

    public GetUserListQueryHandler(IUserRepository userRepo)
    {
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
    }

    public async Task<Result<IReadOnlyList<UserListItemDto>>> Handle(
        GetUserListQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var users = await _userRepo.GetAllByCompanyAsync(request.CompanyId, cancellationToken);

            var dtos = users.Select(u => new UserListItemDto
            {
                Id                 = u.Id,
                DisplayName        = u.DisplayName,
                Username           = u.Username,
                PrimaryRole        = u.Roles.FirstOrDefault()?.RoleName ?? (u.IsSystemAdmin ? "System Admin" : "User"),
                IsActive           = u.IsActive,
                IsSystemAdmin      = u.IsSystemAdmin,
                MustChangePassword = u.MustChangePassword,
                IsLockedOut        = u.IsLockedOut(),
                CreatedAt          = u.CreatedAt,
                LastLoginAt        = u.LastLoginAt
            }).ToList();

            return Result<IReadOnlyList<UserListItemDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<UserListItemDto>>.Failure($"Failed to load users: {ex.Message}");
        }
    }
}
