using System;
using FijiPayroll.Shared.Formula;
using Microsoft.Extensions.Caching.Memory;

namespace FijiPayroll.Infrastructure.Services;

/// <summary>
/// In-memory cache implementation for compiled AST formula rules.
/// </summary>
public sealed class MemoryFormulaCache : IFormulaCache
{
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    /// Initialises a new instance of the <see cref="MemoryFormulaCache"/> class.
    /// </summary>
    public MemoryFormulaCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    /// <inheritdoc/>
    public AstNode GetOrAdd(
        int companyId,
        int? ruleSetId,
        int fiscalYear,
        int componentId,
        int ruleVersion,
        string compiledHash,
        Func<AstNode> compileFunc)
    {
        // Cache key includes: CompanyId, RuleSetId, FiscalYear, ComponentId, RuleVersion, CompiledHash
        var cacheKey = $"BRE_Formula_{companyId}_{ruleSetId ?? 0}_{fiscalYear}_{componentId}_{ruleVersion}_{compiledHash}";

        return _memoryCache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return compileFunc();
        })!;
    }
}
