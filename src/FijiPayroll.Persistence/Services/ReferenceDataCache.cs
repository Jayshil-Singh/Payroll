using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Persistence.Services;

/// <summary>
/// Thread-safe in-memory caching implementation for Reference Data.
/// Automatically handles sliding cache expirations and multi-tenant key mappings.
/// Resides in Persistence to have direct query access to ApplicationDbContext.
/// </summary>
public sealed class ReferenceDataCache : IReferenceDataCache
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<ReferenceDataCache> _logger;

    // Key format: {TenantId}_{CategoryName}
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(10);

    /// <summary>Initializes the reference data cache.</summary>
    public ReferenceDataCache(
        IServiceProvider serviceProvider,
        ITenantProvider tenantProvider,
        ILogger<ReferenceDataCache> logger)
    {
        _serviceProvider = serviceProvider;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MasterLookup>> GetLookupsByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        int tenantId = _tenantProvider.GetCurrentCompanyId();
        string cacheKey = $"{tenantId}_{category.ToUpperInvariant()}";

        if (_cache.TryGetValue(cacheKey, out var entry) && entry.ExpiryTime > DateTime.UtcNow)
        {
            return entry.Items;
        }

        // Resolve context inside transient scope to prevent EF tracking context accumulation
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var items = await context.MasterLookups
            .Where(ml => ml.CompanyId == tenantId && ml.Category == category.ToUpperInvariant() && ml.IsActive)
            .OrderBy(ml => ml.DisplayOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var newEntry = new CacheEntry(items.AsReadOnly(), DateTime.UtcNow.Add(CacheExpiry));
        _cache[cacheKey] = newEntry;

        _logger.LogDebug("[Cache] Loaded reference data category {Category} for Tenant {Tenant}", category, tenantId);
        return newEntry.Items;
    }

    /// <inheritdoc />
    public async Task<MasterLookup?> GetLookupByCodeAsync(string category, string code, CancellationToken cancellationToken = default)
    {
        var list = await GetLookupsByCategoryAsync(category, cancellationToken).ConfigureAwait(false);
        return list.FirstOrDefault(ml => ml.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public void InvalidateCategory(string category)
    {
        int tenantId = _tenantProvider.GetCurrentCompanyId();
        string cacheKey = $"{tenantId}_{category.ToUpperInvariant()}";
        _cache.TryRemove(cacheKey, out _);
        _logger.LogDebug("[Cache] Invalidated category {Category} for Tenant {Tenant}", category, tenantId);
    }

    /// <inheritdoc />
    public void InvalidateAll()
    {
        _cache.Clear();
        _logger.LogDebug("[Cache] Invalidated entire reference cache.");
    }

    private sealed record CacheEntry(IReadOnlyList<MasterLookup> Items, DateTime ExpiryTime);
}
