using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FijiPayroll.Persistence.Repositories;

/// <summary>
/// Entity Framework Core implementation of the <see cref="IPayrollComponentRepository"/> interface.
/// Provides data access operations for <see cref="PayrollComponent"/> entities.
/// </summary>
public sealed class PayrollComponentRepository : IPayrollComponentRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initialises a new instance of the <see cref="PayrollComponentRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public PayrollComponentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<PayrollComponent?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.PayrollComponents
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PayrollComponent>> GetByCompanyAsync(
        int companyId,
        CancellationToken cancellationToken = default)
    {
        var list = await _context.PayrollComponents
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);

        return list.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PayrollComponent>> GetByCompanyAndTypeAsync(
        int companyId,
        ComponentType componentType,
        CancellationToken cancellationToken = default)
    {
        var list = await _context.PayrollComponents
            .Where(x => x.CompanyId == companyId && x.ComponentType == componentType)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);

        return list.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<bool> CodeExistsAsync(
        int companyId,
        string componentCode,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PayrollComponents
            .Where(x => x.CompanyId == companyId && x.ComponentCode == componentCode.Trim().ToUpperInvariant());

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> IsUsedInPayrollRunsAsync(
        int componentId,
        CancellationToken cancellationToken = default)
    {
        // Since payroll run aggregate and tables are not yet implemented in Phase 05,
        // there are no references. This will be updated to query the run component details in Phase 08.
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public async Task AddAsync(PayrollComponent component, CancellationToken cancellationToken = default)
    {
        await _context.PayrollComponents.AddAsync(component, cancellationToken);
    }

    /// <inheritdoc/>
    public void Update(PayrollComponent component)
    {
        _context.PayrollComponents.Update(component);
    }

    /// <inheritdoc/>
    public async Task<int> GetMaxDisplayOrderAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var maxOrder = await _context.PayrollComponents
            .Where(x => x.CompanyId == companyId)
            .Select(x => (int?)x.DisplayOrder)
            .MaxAsync(cancellationToken);

        return maxOrder ?? 0;
    }

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<PayrollComponent> Items, int TotalCount)> GetPagedAsync(
        int companyId,
        string? searchTerm,
        ComponentType? typeFilter,
        bool activeOnly,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PayrollComponents
            .Where(x => x.CompanyId == companyId);

        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        if (typeFilter.HasValue)
        {
            query = query.Where(x => x.ComponentType == typeFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToUpperInvariant();
            query = query.Where(x => x.ComponentCode.Contains(term) || x.ComponentName.Contains(term));
        }

        // Get total count matching criteria
        int totalCount = await query.CountAsync(cancellationToken);

        // Apply ordering and paging
        var items = await query
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.ComponentCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items.AsReadOnly(), totalCount);
    }
}
