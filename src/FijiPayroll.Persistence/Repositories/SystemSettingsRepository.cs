using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ISystemSettingsRepository"/>.
/// </summary>
public sealed class SystemSettingsRepository : ISystemSettingsRepository
{
    private readonly ApplicationDbContext _context;

    public SystemSettingsRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<SystemSettings?> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _context.SystemSettings
            .FirstOrDefaultAsync(x => x.CompanyId == companyId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(SystemSettings settings, CancellationToken cancellationToken = default)
    {
        await _context.SystemSettings.AddAsync(settings, cancellationToken);
    }

    /// <inheritdoc/>
    public void Update(SystemSettings settings)
    {
        _context.SystemSettings.Update(settings);
    }
}
