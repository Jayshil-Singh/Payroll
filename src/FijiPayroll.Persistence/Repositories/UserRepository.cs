using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Persistence.Repositories;

/// <summary>
/// Entity Framework Core implementation of the IUserRepository.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<UserAccount?> GetByUsernameAsync(string username, int companyId, CancellationToken ct)
    {
        return await _context.UserAccounts
            .Include(u => u.Roles)
                .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.Username == username && u.CompanyId == companyId, ct);
    }

    /// <inheritdoc />
    public async Task<UserAccount?> GetByIdAsync(int userId, CancellationToken ct)
    {
        return await _context.UserAccounts
            .Include(u => u.Roles)
                .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    /// <inheritdoc />
    public async Task AddAsync(UserAccount user, CancellationToken ct)
    {
        await _context.UserAccounts.AddAsync(user, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetPermissionsForUserAsync(int userId, CancellationToken ct)
    {
        return await _context.UserRoles
            .Where(r => r.UserAccountId == userId)
            .SelectMany(r => r.Permissions)
            .Select(p => p.PermissionCode)
            .Distinct()
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Company>> GetCompaniesByUsernameAsync(string username, CancellationToken ct)
    {
        var companyIds = await _context.UserAccounts
            .IgnoreQueryFilters()
            .Where(u => u.Username == username && u.IsActive)
            .Select(u => u.CompanyId)
            .Distinct()
            .ToListAsync(ct);

        return await _context.Companies
            .IgnoreQueryFilters()
            .Where(c => companyIds.Contains(c.Id) && !c.IsDeleted && c.IsActive)
            .ToListAsync(ct);
    }
}
