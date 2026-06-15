using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Persistence.Repositories;

/// <summary>
/// EF Core repository implementation for the <see cref="SearchIndex"/> entity.
/// </summary>
public sealed class SearchIndexRepository : ISearchIndexRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>Initializes the repository with the DbContext.</summary>
    public SearchIndexRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task AddAsync(SearchIndex index, CancellationToken cancellationToken)
    {
        await _context.Set<SearchIndex>().AddAsync(index, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(SearchIndex index)
    {
        _context.Set<SearchIndex>().Update(index);
    }

    /// <inheritdoc />
    public async Task<SearchIndex?> GetByEntityAsync(int companyId, string entityType, string entityId, CancellationToken cancellationToken)
    {
        return await _context.Set<SearchIndex>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId &&
                                      x.EntityType == entityType &&
                                      x.EntityId == entityId,
                                 cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<SearchIndex>> GetByCompanyAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.Set<SearchIndex>()
            .IgnoreQueryFilters()
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<SearchIndex>> SearchLikeAsync(int companyId, string[] queryTerms, CancellationToken cancellationToken)
    {
        IQueryable<SearchIndex> queryable = _context.Set<SearchIndex>()
            .IgnoreQueryFilters()
            .Where(x => x.CompanyId == companyId);

        foreach (var term in queryTerms)
        {
            queryable = queryable.Where(x => x.Content.Contains(term));
        }

        return await queryable.ToListAsync(cancellationToken);
    }
}
