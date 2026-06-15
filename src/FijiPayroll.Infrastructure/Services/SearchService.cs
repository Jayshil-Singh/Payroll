using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FijiPayroll.Infrastructure.Services;

/// <summary>
/// Structured data stored in the SearchIndex.Content property.
/// </summary>
public sealed class SearchContentData
{
    /// <summary>Display title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Display snippet.</summary>
    public string Snippet { get; set; } = string.Empty;

    /// <summary>Employee full name.</summary>
    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>Department name.</summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>Notes or branch/position details.</summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>Other details (TIN, FNPF, Email, etc.).</summary>
    public string Other { get; set; } = string.Empty;
}

/// <summary>
/// Hybrid cache search entry holding both raw entity and pre-parsed search content.
/// </summary>
internal sealed class CachedSearchEntry
{
    public SearchIndex Index { get; }
    public SearchContentData Data { get; }

    public CachedSearchEntry(SearchIndex index, SearchContentData data)
    {
        Index = index;
        Data = data;
    }
}

/// <summary>
/// Multi-tenant fuzzy search service using SQL table + memory cache hybrid indexer.
/// </summary>
public sealed class SearchService : ISearchService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SearchService> _logger;

    private readonly Channel<IndexTask> _channel;
    private readonly CancellationTokenSource _cts;
    private readonly Task _processorTask;

    // Cache mapping CompanyId -> list of cached entries
    private readonly ConcurrentDictionary<int, List<CachedSearchEntry>> _cache = new();

    private sealed record IndexTask(
        string EntityType,
        string EntityId,
        string DataJson,
        int CompanyId,
        DateTime LastUpdated);

    /// <summary>
    /// Initializes a new instance of SearchService.
    /// </summary>
    public SearchService(
        IServiceScopeFactory scopeFactory,
        ITenantProvider tenantProvider,
        ILogger<SearchService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _channel = Channel.CreateUnbounded<IndexTask>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _cts = new CancellationTokenSource();
        _processorTask = Task.Run(() => ProcessQueueAsync(_cts.Token));
    }

    /// <inheritdoc />
    public async Task IndexEntityAsync(string entityType, string entityId, string data, CancellationToken cancellationToken = default)
    {
        int companyId = _tenantProvider.GetCurrentCompanyId();
        var task = new IndexTask(entityType, entityId, data, companyId, DateTime.UtcNow);

        await _channel.Writer.WriteAsync(task, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, int maxResults, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<SearchResult>();
        }

        int companyId = _tenantProvider.GetCurrentCompanyId();
        await EnsureCacheLoadedAsync(companyId, cancellationToken);

        var queryTerms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (queryTerms.Length == 0)
        {
            return Array.Empty<SearchResult>();
        }

        var results = new List<SearchResult>();

        if (_cache.TryGetValue(companyId, out var cachedEntries))
        {
            lock (cachedEntries)
            {
                foreach (var entry in cachedEntries)
                {
                    if (TryMatchEntry(entry, queryTerms, out double rankScore))
                    {
                        results.Add(new SearchResult(
                            entry.Index.EntityType,
                            entry.Index.EntityId,
                            entry.Data.Title,
                            entry.Data.Snippet,
                            rankScore));
                    }
                }
            }
        }
        else
        {
            // Fallback SQL search if cache is not loaded or fails
            _logger.LogWarning("Search Cache unavailable for company {CompanyId}. Falling back to SQL search.", companyId);
            results = await SqlSearchFallbackAsync(companyId, queryTerms, cancellationToken);
        }

        return results
            .OrderByDescending(r => r.RankScore)
            .Take(maxResults)
            .ToList();
    }

    private async Task EnsureCacheLoadedAsync(int companyId, CancellationToken cancellationToken)
    {
        if (_cache.ContainsKey(companyId))
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var dbIndexes = await unitOfWork.SearchIndexes.GetByCompanyAsync(companyId, cancellationToken);

            var list = new List<CachedSearchEntry>();
            foreach (var index in dbIndexes)
            {
                var data = ParseContent(index.Content, index.EntityType, index.EntityId);
                list.Add(new CachedSearchEntry(index, data));
            }

            _cache.TryAdd(companyId, list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pre-load search index cache for company {CompanyId}", companyId);
        }
    }

    private async Task<List<SearchResult>> SqlSearchFallbackAsync(int companyId, string[] queryTerms, CancellationToken cancellationToken)
    {
        var results = new List<SearchResult>();
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var dbMatches = await unitOfWork.SearchIndexes.SearchLikeAsync(companyId, queryTerms, cancellationToken);

            foreach (var index in dbMatches)
            {
                var data = ParseContent(index.Content, index.EntityType, index.EntityId);
                var entry = new CachedSearchEntry(index, data);

                if (TryMatchEntry(entry, queryTerms, out double rankScore))
                {
                    results.Add(new SearchResult(
                        index.EntityType,
                        index.EntityId,
                        data.Title,
                        data.Snippet,
                        rankScore));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL Fallback search failed for company {CompanyId}", companyId);
        }

        return results;
    }

    private bool TryMatchEntry(CachedSearchEntry entry, string[] queryTerms, out double rankScore)
    {
        rankScore = entry.Index.WeightedScore; // start with base weight stored in DB
        bool matchedAny = false;
        double maxFuzzyBonus = 0.0;

        foreach (var term in queryTerms)
        {
            bool termMatched = false;

            // Check EmployeeName
            if (!string.IsNullOrEmpty(entry.Data.EmployeeName))
            {
                if (entry.Data.EmployeeName.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    rankScore += 10.0;
                    maxFuzzyBonus = Math.Max(maxFuzzyBonus, 5.0);
                    termMatched = true;
                }
                else if (LevenshteinDistance.IsFuzzyMatch(term, entry.Data.EmployeeName, out _))
                {
                    rankScore += 10.0;
                    maxFuzzyBonus = Math.Max(maxFuzzyBonus, 2.0);
                    termMatched = true;
                }
            }

            // Check Department
            if (!string.IsNullOrEmpty(entry.Data.Department))
            {
                if (entry.Data.Department.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    rankScore += 5.0;
                    maxFuzzyBonus = Math.Max(maxFuzzyBonus, 5.0);
                    termMatched = true;
                }
                else if (LevenshteinDistance.IsFuzzyMatch(term, entry.Data.Department, out _))
                {
                    rankScore += 5.0;
                    maxFuzzyBonus = Math.Max(maxFuzzyBonus, 2.0);
                    termMatched = true;
                }
            }

            // Check Notes
            if (!string.IsNullOrEmpty(entry.Data.Notes))
            {
                if (entry.Data.Notes.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    rankScore += 3.0;
                    maxFuzzyBonus = Math.Max(maxFuzzyBonus, 5.0);
                    termMatched = true;
                }
                else if (LevenshteinDistance.IsFuzzyMatch(term, entry.Data.Notes, out _))
                {
                    rankScore += 3.0;
                    maxFuzzyBonus = Math.Max(maxFuzzyBonus, 2.0);
                    termMatched = true;
                }
            }

            // Check Other
            if (!string.IsNullOrEmpty(entry.Data.Other))
            {
                if (entry.Data.Other.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    rankScore += 1.0;
                    maxFuzzyBonus = Math.Max(maxFuzzyBonus, 5.0);
                    termMatched = true;
                }
                else if (LevenshteinDistance.IsFuzzyMatch(term, entry.Data.Other, out _))
                {
                    rankScore += 1.0;
                    maxFuzzyBonus = Math.Max(maxFuzzyBonus, 2.0);
                    termMatched = true;
                }
            }

            if (termMatched)
            {
                matchedAny = true;
            }
        }

        if (!matchedAny)
        {
            return false;
        }

        rankScore += maxFuzzyBonus;

        // Recency Boost
        var age = DateTime.UtcNow - entry.Index.LastUpdated;
        if (age.TotalDays <= 1) rankScore += 5.0;
        else if (age.TotalDays <= 7) rankScore += 3.0;
        else if (age.TotalDays <= 30) rankScore += 1.0;

        return true;
    }

    private static SearchContentData ParseContent(string content, string entityType, string entityId)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new SearchContentData();
        }

        if (content.TrimStart().StartsWith("{"))
        {
            try
            {
                return JsonSerializer.Deserialize<SearchContentData>(content) ?? new SearchContentData();
            }
            catch
            {
                // Fallback on deserialization failure
            }
        }

        return new SearchContentData
        {
            Title = $"{entityType} {entityId}",
            Snippet = content,
            Other = content
        };
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var task in _channel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await IndexItemAsync(task, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing search index queue item: {EntityType} {EntityId}", task.EntityType, task.EntityId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Search Index processor task was cancelled.");
        }
    }

    private async Task IndexItemAsync(IndexTask task, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Check if index already exists
        var index = await unitOfWork.SearchIndexes.GetByEntityAsync(task.CompanyId, task.EntityType, task.EntityId, cancellationToken);

        int baseWeight = task.EntityType == "Employee" ? 10 : 1;

        if (index != null)
        {
            index.Update(task.DataJson, baseWeight, task.LastUpdated);
            unitOfWork.SearchIndexes.Update(index);
        }
        else
        {
            index = SearchIndex.Create(
                Guid.NewGuid(),
                task.EntityType,
                task.EntityId,
                task.DataJson,
                baseWeight,
                task.LastUpdated,
                task.CompanyId);

            await unitOfWork.SearchIndexes.AddAsync(index, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Update in-memory cache
        var parsedData = ParseContent(task.DataJson, task.EntityType, task.EntityId);
        var cacheList = _cache.GetOrAdd(task.CompanyId, _ => new List<CachedSearchEntry>());

        lock (cacheList)
        {
            int idx = cacheList.FindIndex(x => x.Index.EntityType == task.EntityType && x.Index.EntityId == task.EntityId);
            if (idx >= 0)
            {
                cacheList[idx] = new CachedSearchEntry(index, parsedData);
            }
            else
            {
                cacheList.Add(new CachedSearchEntry(index, parsedData));
            }
        }
    }

    private bool _disposed;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        try
        {
            _channel.Writer.Complete();
            _processorTask.GetAwaiter().GetResult();
        }
        catch
        {
            // Suppress exceptions on shutdown
        }

        try
        {
            _cts.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }
    }
}
