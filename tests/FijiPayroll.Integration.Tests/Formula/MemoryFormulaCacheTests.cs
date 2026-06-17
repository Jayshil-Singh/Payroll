using FijiPayroll.Infrastructure.Services;
using FijiPayroll.Shared.Formula;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using System;
using Xunit;

namespace FijiPayroll.Integration.Tests.Formula;

/// <summary>
/// Integration unit tests for the MemoryFormulaCache verification, confirming hit/miss logic and tenant isolation.
/// </summary>
public sealed class MemoryFormulaCacheTests
{
    [Fact]
    public void GetOrAdd_FirstCall_CompilesAndCaches()
    {
        // Arrange
        var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new MemoryCacheOptions());
        var formulaCache = new MemoryFormulaCache(memoryCache);

        int callCount = 0;
        Func<AstNode> compileFunc = () =>
        {
            callCount++;
            return new NumberNode(100m);
        };

        // Act
        var node1 = formulaCache.GetOrAdd(1, 10, 2026, 101, 1, "hash1", compileFunc);
        var node2 = formulaCache.GetOrAdd(1, 10, 2026, 101, 1, "hash1", compileFunc);

        // Assert
        node1.Should().BeSameAs(node2);
        callCount.Should().Be(1);
    }

    [Fact]
    public void GetOrAdd_DifferentCompanies_IsolatesCacheEntries()
    {
        // Arrange
        var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new MemoryCacheOptions());
        var formulaCache = new MemoryFormulaCache(memoryCache);

        int callCount = 0;
        Func<AstNode> compileFunc = () =>
        {
            callCount++;
            return new NumberNode(100m);
        };

        // Act
        var node1 = formulaCache.GetOrAdd(1, 10, 2026, 101, 1, "hash1", compileFunc);
        var node2 = formulaCache.GetOrAdd(2, 10, 2026, 101, 1, "hash1", compileFunc);

        // Assert
        node1.Should().NotBeSameAs(node2);
        callCount.Should().Be(2);
    }

    [Fact]
    public void GetOrAdd_DifferentVersions_Recompiles()
    {
        // Arrange
        var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new MemoryCacheOptions());
        var formulaCache = new MemoryFormulaCache(memoryCache);

        int callCount = 0;
        Func<AstNode> compileFunc = () =>
        {
            callCount++;
            return new NumberNode(100m);
        };

        // Act
        var node1 = formulaCache.GetOrAdd(1, 10, 2026, 101, 1, "hash1", compileFunc);
        var node2 = formulaCache.GetOrAdd(1, 10, 2026, 101, 2, "hash1", compileFunc); // version changed

        // Assert
        node1.Should().NotBeSameAs(node2);
        callCount.Should().Be(2);
    }
}
