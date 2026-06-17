using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Features.Employees.EventHandlers;
using FijiPayroll.Application.Features.Search.Queries;
using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Events;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Infrastructure.Services;
using FijiPayroll.Persistence.Context;
using FijiPayroll.Persistence.Interceptors;
using FijiPayroll.Persistence.Repositories;
using FijiPayroll.Shared.Utilities;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FijiPayroll.Integration.Tests;

public sealed class SearchIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ISearchService _searchService;

    public SearchIntegrationTests()
    {
        var services = new ServiceCollection();

        // Database setup
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: dbName));

        // Mocks & Providers
        _tenantProvider = Substitute.For<ITenantProvider>();
        _tenantProvider.GetCurrentCompanyId().Returns(1);
        services.AddSingleton(_tenantProvider);

        var currentUserAccessor = Substitute.For<ICurrentUserAccessor>();
        currentUserAccessor.Username.Returns("search-test-user");
        services.AddSingleton(currentUserAccessor);

        var auditableInterceptor = new AuditableEntityInterceptor(currentUserAccessor);
        services.AddSingleton(auditableInterceptor);

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

        // Register the real SearchService
        services.AddSingleton<ISearchService, SearchService>();

        // Register Loggers
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _searchService = _serviceProvider.GetRequiredService<ISearchService>();
    }

    [Theory]
    [InlineData("Fiji", "Fiji", true, 0)]      // Exact
    [InlineData("Fiji", "Fij", true, 1)]       // Length <= 5, dist 1
    [InlineData("Fiji", "Fijii", true, 1)]     // Length <= 5, dist 1
    [InlineData("Fiji", "Fi", true, 2)]        // Length <= 5, dist 2
    [InlineData("Fiji", "F", false, 3)]        // Length <= 5, dist 3 (exceeds 2)
    [InlineData("SuvaBranchOffice", "SuvaBranchOffic", true, 1)]  // Length > 5, dist 1 (<= 20% of 16)
    [InlineData("SuvaBranchOffice", "SuvaBranchOff", true, 3)]    // Length > 5, dist 3 (<= 20% of 16 is 3)
    [InlineData("SuvaBranchOffice", "SuvaBranch", false, 6)]      // Length > 5, dist 6 (> 20% of 16 is 3)
    public void LevenshteinDistance_Rules_MatchExpectedThresholds(string s1, string s2, bool shouldMatch, int expectedDist)
    {
        // Act
        int distance = LevenshteinDistance.Calculate(s1, s2);
        bool isMatch = LevenshteinDistance.IsFuzzyMatch(s1, s2, out int outDist);

        // Assert
        distance.Should().Be(expectedDist);
        outDist.Should().Be(expectedDist);
        isMatch.Should().Be(shouldMatch);
    }

    [Fact]
    public async Task SearchService_IndexesAndRetrievesAsynchronously()
    {
        // Arrange
        var employee = Employee.Create(
            companyId: 1,
            fullName: "Albert Prasad",
            tin: "123456789",
            fnpfNumber: "88888-A",
            residencyStatus: "Resident",
            department: "Engineering",
            baseSalary: 4000m,
            frequency: FijiPayroll.Domain.Enumerations.PayrollFrequencyType.Monthly,
            isFnpfExempt: false,
            isTaxExempt: false,
            isActive: true,
            branch: "Suva",
            position: "Developer",
            email: "albert@company.com"
        );

        var logger = Substitute.For<ILogger<EmployeeSearchIndexHandler>>();
        var handler = new EmployeeSearchIndexHandler(_searchService, logger);
        var wrapper = new MediatRNotificationWrapper<EmployeeCreatedEvent>(new EmployeeCreatedEvent(employee));

        // Act
        await handler.Handle(wrapper, CancellationToken.None);

        // Allow background queue to process
        await Task.Delay(150);

        // Assert
        var results = await _searchService.SearchAsync("Albert", 10, CancellationToken.None);

        results.Should().NotBeEmpty();
        results.First().Title.Should().Be("Albert Prasad");
        results.First().EntityType.Should().Be("Employee");
        results.First().Snippet.Should().Contain("Developer - Engineering (Suva)");
    }

    [Fact]
    public async Task SearchService_EnforcesTenantIsolation()
    {
        // Arrange
        // Index employee for Company 1
        _tenantProvider.GetCurrentCompanyId().Returns(1);
        await _searchService.IndexEntityAsync("Employee", "101", "{\"Title\":\"Company One Worker\",\"EmployeeName\":\"One Worker\"}");

        // Index employee for Company 2
        _tenantProvider.GetCurrentCompanyId().Returns(2);
        await _searchService.IndexEntityAsync("Employee", "102", "{\"Title\":\"Company Two Worker\",\"EmployeeName\":\"Two Worker\"}");

        await Task.Delay(150);

        // Act & Assert
        // Search as Company 1
        _tenantProvider.GetCurrentCompanyId().Returns(1);
        var results1 = await _searchService.SearchAsync("Worker", 10, CancellationToken.None);
        results1.Should().HaveCount(1);
        results1.First().Title.Should().Be("Company One Worker");

        // Search as Company 2
        _tenantProvider.GetCurrentCompanyId().Returns(2);
        var results2 = await _searchService.SearchAsync("Worker", 10, CancellationToken.None);
        results2.Should().HaveCount(1);
        results2.First().Title.Should().Be("Company Two Worker");
    }

    [Fact]
    public async Task SearchService_WeightedRanking_AppliesPrecedence()
    {
        // Arrange
        _tenantProvider.GetCurrentCompanyId().Returns(1);

        // Entry A: Matches name (Weight 10)
        await _searchService.IndexEntityAsync("Employee", "A", "{\"Title\":\"Target Match\",\"EmployeeName\":\"Target Match\",\"Department\":\"Other\"}");
        // Entry B: Matches department (Weight 5)
        await _searchService.IndexEntityAsync("Employee", "B", "{\"Title\":\"Some Employee\",\"EmployeeName\":\"Other Name\",\"Department\":\"Target Match\"}");
        // Entry C: Matches notes (Weight 3)
        await _searchService.IndexEntityAsync("Employee", "C", "{\"Title\":\"Another Employee\",\"EmployeeName\":\"Other Name\",\"Notes\":\"Target Match\"}");
        // Entry D: Matches other (Weight 1)
        await _searchService.IndexEntityAsync("Employee", "D", "{\"Title\":\"Last Employee\",\"EmployeeName\":\"Other Name\",\"Other\":\"Target Match\"}");

        await Task.Delay(150);

        // Act
        var results = await _searchService.SearchAsync("Target Match", 10, CancellationToken.None);

        // Assert
        results.Should().HaveCount(4);
        results[0].EntityId.Should().Be("A"); // Name match (Weight 10)
        results[1].EntityId.Should().Be("B"); // Dept match (Weight 5)
        results[2].EntityId.Should().Be("C"); // Notes match (Weight 3)
        results[3].EntityId.Should().Be("D"); // Other match (Weight 1)
    }

    [Fact]
    public async Task SearchService_RecencyBoost_FavorsRecentlyUpdated()
    {
        // Arrange
        _tenantProvider.GetCurrentCompanyId().Returns(1);

        // We will insert index records directly to control their LastUpdated timestamps
        var oldDate = DateTime.UtcNow.AddDays(-15);
        var newDate = DateTime.UtcNow;

        var indexOld = SearchIndex.Create(
            Guid.NewGuid(),
            "Employee",
            "OLD",
            "{\"Title\":\"Fiji Office Worker\",\"EmployeeName\":\"Office Worker\"}",
            10,
            oldDate,
            1
        );

        var indexNew = SearchIndex.Create(
            Guid.NewGuid(),
            "Employee",
            "NEW",
            "{\"Title\":\"Fiji Office Worker\",\"EmployeeName\":\"Office Worker\"}",
            10,
            newDate,
            1
        );

        await _context.SearchIndexes.AddRangeAsync(indexOld, indexNew);
        await _context.SaveChangesAsync();

        // Act
        // This will bypass the queue and force loading them from database into cache
        var results = await _searchService.SearchAsync("Office Worker", 10, CancellationToken.None);

        // Assert
        results.Should().HaveCount(2);
        results[0].EntityId.Should().Be("NEW"); // Recency boost makes it higher score
        results[1].EntityId.Should().Be("OLD");
    }

    [Fact]
    public async Task SearchQuery_MediatRQueryHandler_WorksCorrectly()
    {
        // Arrange
        _tenantProvider.GetCurrentCompanyId().Returns(1);
        await _searchService.IndexEntityAsync("Employee", "99", "{\"Title\":\"Query User\",\"EmployeeName\":\"Query User\"}");
        await Task.Delay(150);

        var query = new SearchQuery("Query User", 10);
        var handler = new SearchQueryHandler(_searchService);

        // Act
        var results = await handler.Handle(query, CancellationToken.None);

        // Assert
        results.Should().NotBeEmpty();
        results.First().Title.Should().Be("Query User");
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
