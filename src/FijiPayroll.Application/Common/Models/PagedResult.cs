namespace FijiPayroll.Application.Common.Models;

/// <summary>
/// Model representing paginated query results with metadata for grid display in the WPF UI.
/// Used to enforce server-side pagination.
/// </summary>
/// <typeparam name="T">The DTO type of each page item.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>The items on the current page.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>The 1-based current page number.</summary>
    public int PageNumber { get; }

    /// <summary>Maximum items per page.</summary>
    public int PageSize { get; }

    /// <summary>Total number of records matching the query (across all pages).</summary>
    public int TotalCount { get; }

    /// <summary>Total number of pages.</summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary><c>true</c> if there is a page before the current one.</summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary><c>true</c> if there is a page after the current one.</summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>Initialises a paged result.</summary>
    public PagedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
