using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Infrastructure.Services;
using FijiPayroll.Persistence.Context;
using FijiPayroll.Persistence.Interceptors;
using FijiPayroll.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FijiPayroll.Integration.Tests.Performance;

public sealed class FuzzySearchStressTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ISearchService _searchService;

    public FuzzySearchStressTests()
    {
        var services = new ServiceCollection();

        // Database setup (unique name per test)
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: dbName));

        // Mocks
        _tenantProvider = Substitute.For<ITenantProvider>();
        _tenantProvider.GetCurrentCompanyId().Returns(1);
        services.AddSingleton(_tenantProvider);

        var currentUserAccessor = Substitute.For<ICurrentUserAccessor>();
        currentUserAccessor.Username.Returns("stress-test-user");
        services.AddSingleton(currentUserAccessor);

        // Register the repositories (Mock everything except ISearchIndexRepository)
        services.AddScoped<IPayrollComponentRepository>(sp => Substitute.For<IPayrollComponentRepository>());
        services.AddScoped<IPayrollRunRepository>(sp => Substitute.For<IPayrollRunRepository>());
        services.AddScoped<IEmployeeRepository>(sp => Substitute.For<IEmployeeRepository>());
        services.AddScoped<ITaxBracketRepository>(sp => Substitute.For<ITaxBracketRepository>());
        services.AddScoped<IMasterLookupRepository>(sp => Substitute.For<IMasterLookupRepository>());
        services.AddScoped<IImportJobRepository>(sp => Substitute.For<IImportJobRepository>());
        services.AddScoped<ISearchIndexRepository, SearchIndexRepository>();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Real SearchService
        services.AddSingleton<ISearchService, SearchService>();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _searchService = _serviceProvider.GetRequiredService<ISearchService>();
    }

    [Fact]
    public async Task SearchAsync_StressAndAllocationSafety_UnderLargeDataset()
    {
        // 1. Seed dataset of 10,000 employees directly into database to bypass queue latency
        var indexEntries = new List<SearchIndex>(10000);
        for (int i = 0; i < 10000; i++)
        {
            var content = $"{{\"Title\":\"Employee Name {i}\",\"EmployeeName\":\"Employee Name {i}\",\"Department\":\"Engineering\",\"Notes\":\"Position details or notes {i}\",\"Other\":\"TIN123456{i} FNPF888{i}\"}}";
            indexEntries.Add(SearchIndex.Create(
                Guid.NewGuid(),
                "Employee",
                $"EMP-{i}",
                content,
                weightedScore: i % 2 == 0 ? 10 : 5,
                DateTime.UtcNow.AddHours(-i % 48), // varying last updated for ranking
                companyId: 1
            ));
        }

        await _context.SearchIndexes.AddRangeAsync(indexEntries);
        await _context.SaveChangesAsync();

        // 2. Warm up search cache
        var warmUpResults = await _searchService.SearchAsync("Employee Name 5000", 10, CancellationToken.None);
        warmUpResults.Should().NotBeEmpty();

        // Force GC collection to start from a clean slate
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();

        // 3. Track allocations during multiple fuzzy search runs
        long startAllocatedBytes = GC.GetAllocatedBytesForCurrentThread();

        var searchWatch = Stopwatch.StartNew();
        for (int i = 0; i < 50; i++)
        {
            // Perform fuzzy search queries (levenshtein calculations on 10,000 entries)
            var searchResults = await _searchService.SearchAsync($"Employee Nmae {i * 100}", 10, CancellationToken.None);
            searchResults.Should().NotBeEmpty();
        }
        searchWatch.Stop();

        long endAllocatedBytes = GC.GetAllocatedBytesForCurrentThread();
        long totalBytesAllocated = endAllocatedBytes - startAllocatedBytes;
        double averageLatencyMs = searchWatch.ElapsedMilliseconds / 50.0;

        // Verify allocation footprint and latency is low and bounded
        totalBytesAllocated.Should().BeLessThan(80 * 1024 * 1024); // Cap allocations to prevent LOH spikes
        averageLatencyMs.Should().BeLessThan(350); // Typical local fuzzy match calculations should be fast

        // 4. Concurrent Search Thread Safety Audit
        int concurrentTasksCount = 20;
        var concurrentTasks = new List<Task<IReadOnlyList<SearchResult>>>(concurrentTasksCount);

        var concurrentWatch = Stopwatch.StartNew();
        for (int i = 0; i < concurrentTasksCount; i++)
        {
            int targetIdx = (i * 450) % 10000;
            // Fuzzy search target with spelling mistake
            string query = $"Empolyee Name {targetIdx}"; 
            concurrentTasks.Add(_searchService.SearchAsync(query, 5, CancellationToken.None));
        }

        var allResults = await Task.WhenAll(concurrentTasks);
        concurrentWatch.Stop();

        // Verify all threads returned results successfully
        allResults.Length.Should().Be(concurrentTasksCount);
        foreach (var resultsSet in allResults)
        {
            resultsSet.Should().NotBeNull();
        }

        // Concurrency target check
        concurrentWatch.ElapsedMilliseconds.Should().BeLessThan(1500); // 20 threads concurrent searches finished quickly
    }

    public void Dispose()
    {
        if (_searchService is IDisposable disp)
        {
            disp.Dispose();
        }
        _context.Dispose();
        _serviceProvider.Dispose();
    }
}
