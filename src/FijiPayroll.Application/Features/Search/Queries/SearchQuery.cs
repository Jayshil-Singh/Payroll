using FijiPayroll.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Search.Queries;

/// <summary>
/// MediatR query to perform ranked fuzzy search across indexed enterprise entities.
/// </summary>
public sealed record SearchQuery(string QueryText, int MaxResults = 50) : IRequest<IReadOnlyList<SearchResult>>;

/// <summary>
/// Handler for <see cref="SearchQuery"/>.
/// </summary>
public sealed class SearchQueryHandler : IRequestHandler<SearchQuery, IReadOnlyList<SearchResult>>
{
    private readonly ISearchService _searchService;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public SearchQueryHandler(ISearchService searchService)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchResult>> Handle(SearchQuery request, CancellationToken cancellationToken)
    {
        return await _searchService.SearchAsync(request.QueryText, request.MaxResults, cancellationToken);
    }
}
