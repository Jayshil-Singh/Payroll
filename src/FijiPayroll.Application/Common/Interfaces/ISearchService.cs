using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Common.Interfaces;

/// <summary>
/// Model for ranked fuzzy search outcomes.
/// </summary>
public sealed record SearchResult(string EntityType, string EntityId, string Title, string Snippet, double RankScore);

/// <summary>
/// Contract for multi-entity fuzzy text search.
/// Indexing updates can be performed in the background; searches return ranked results.
/// </summary>
public interface ISearchService
{
    /// <summary>Updates or inserts the search terms for an entity in the index.</summary>
    Task IndexEntityAsync(string entityType, string entityId, string data, CancellationToken cancellationToken = default);

    /// <summary>Searches the index, applying weighted ranking and fuzzy term queries.</summary>
    Task<IReadOnlyList<SearchResult>> SearchAsync(string query, int maxResults, CancellationToken cancellationToken = default);
}
