using FijiPayroll.Domain.Entities.Common;
using System;

namespace FijiPayroll.Domain.Entities.Audit;

/// <summary>
/// Domain entity representing a search index entry for fast multi-tenant fuzzy text searches.
/// </summary>
public sealed class SearchIndex : BaseEntity
{
    private SearchIndex() { }

    /// <summary>Gets the unique Search entry ID Guid.</summary>
    public Guid SearchId { get; private set; }

    /// <summary>Gets the indexed entity type name (e.g. Employee).</summary>
    public string EntityType { get; private set; } = string.Empty;

    /// <summary>Gets the indexed entity's string primary key.</summary>
    public string EntityId { get; private set; } = string.Empty;

    /// <summary>Gets the indexed searchable text content.</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>Gets the base weight/importance score of this search record.</summary>
    public int WeightedScore { get; private set; }

    /// <summary>Gets the UTC timestamp when this index was last updated.</summary>
    public DateTime LastUpdated { get; private set; }

    /// <summary>Gets the owner company ID.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Factory method to create a new SearchIndex entry.</summary>
    public static SearchIndex Create(
        Guid searchId,
        string entityType,
        string entityId,
        string content,
        int weightedScore,
        DateTime lastUpdated,
        int companyId)
    {
        return new SearchIndex
        {
            SearchId = searchId,
            EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType)),
            EntityId = entityId ?? throw new ArgumentNullException(nameof(entityId)),
            Content = content ?? string.Empty,
            WeightedScore = weightedScore,
            LastUpdated = lastUpdated,
            CompanyId = companyId
        };
    }

    /// <summary>Updates the searchable content and weight score.</summary>
    public void Update(string content, int weightedScore, DateTime lastUpdated)
    {
        Content = content ?? string.Empty;
        WeightedScore = weightedScore;
        LastUpdated = lastUpdated;
    }
}
