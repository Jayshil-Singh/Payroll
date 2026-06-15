using FijiPayroll.Domain.Entities.Audit;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Domain.Interfaces;

/// <summary>
/// Repository abstraction for SearchIndex aggregate.
/// </summary>
public interface ISearchIndexRepository
{
    /// <summary>Adds a search index record.</summary>
    Task AddAsync(SearchIndex index, CancellationToken cancellationToken);

    /// <summary>Updates a search index record.</summary>
    void Update(SearchIndex index);

    /// <summary>Retrieves a search index record by its multi-tenant natural key.</summary>
    Task<SearchIndex?> GetByEntityAsync(int companyId, string entityType, string entityId, CancellationToken cancellationToken);

    /// <summary>Retrieves all search index records for a given company.</summary>
    Task<List<SearchIndex>> GetByCompanyAsync(int companyId, CancellationToken cancellationToken);

    /// <summary>Retrieves search index records matching query terms using SQL LIKE fallback.</summary>
    Task<List<SearchIndex>> SearchLikeAsync(int companyId, string[] queryTerms, CancellationToken cancellationToken);
}
