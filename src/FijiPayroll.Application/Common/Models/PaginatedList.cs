namespace FijiPayroll.Application.Common.Models;

/// <summary>
/// Carries paginated query results with metadata for grid display in the WPF UI.
/// </summary>
/// <typeparam name="T">The DTO type of each page item.</typeparam>
public sealed class PaginatedList<T>
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

    /// <summary>Initialises a paginated list.</summary>
    public PaginatedList(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items       = items;
        TotalCount  = totalCount;
        PageNumber  = pageNumber;
        PageSize    = pageSize;
    }

    /// <summary>
    /// Creates a <see cref="PaginatedList{T}"/> from an in-memory sequence.
    /// Use for small datasets; for large datasets use the database-paged overload.
    /// </summary>
    public static PaginatedList<T> Create(IEnumerable<T> source, int pageNumber, int pageSize)
    {
        var list       = source.ToList();
        var totalCount = list.Count;
        var items      = list.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return new PaginatedList<T>(items, totalCount, pageNumber, pageSize);
    }
}
