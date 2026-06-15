using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.PayrollComponents.Commands.CreatePayrollComponent;
using FijiPayroll.Application.Features.PayrollComponents.Queries.GetPayrollComponentList;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using FijiPayroll.Persistence.Interceptors;
using FijiPayroll.Persistence.Repositories;
using FijiPayroll.Persistence.Seeders;
using FijiPayroll.Shared.Constants;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FijiPayroll.Integration.Tests;

/// <summary>
/// Integration tests verifying the end-to-end flow from CQRS handlers to persistence,
/// database seeding, and auditing logic.
/// </summary>
public sealed class PayrollComponentIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPayrollComponentRepository _repository;

    public PayrollComponentIntegrationTests()
    {
        // 1. Mock Accessors
        _currentUserAccessor = Substitute.For<ICurrentUserAccessor>();
        _currentUserAccessor.Username.Returns("integration-test-user");

        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.Username.Returns("integration-test-user");
        _currentUserService.HasPermission(Arg.Any<string>()).Returns(true);
        _currentUserService.HasCompanyAccess(Arg.Any<int>()).Returns(true);

        _dateTimeService = Substitute.For<IDateTimeService>();
        _dateTimeService.UtcNow.Returns(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        // 2. Setup EF Core In-Memory Context with Auditing Interceptor
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentCompanyId().Returns(1);

        var interceptor = new AuditableEntityInterceptor(_currentUserAccessor);
        _context = new ApplicationDbContext(options, interceptor, tenantProvider);

        // 3. Setup Persistence Infrastructure
        _repository = new PayrollComponentRepository(_context);
        var mockRunRepo = Substitute.For<IPayrollRunRepository>();
        var mockEmpRepo = Substitute.For<IEmployeeRepository>();
        var mockTaxRepo = Substitute.For<ITaxBracketRepository>();
        var mockLookupRepo = Substitute.For<IMasterLookupRepository>();
        _unitOfWork = new UnitOfWork(_context, _repository, mockRunRepo, mockEmpRepo, mockTaxRepo, mockLookupRepo);
    }

    [Fact]
    public async Task Seeder_IsIdempotent_PopulatesDatabaseExactlyOnce()
    {
        // Arrange
        var seeder = new PayrollComponentSeeder(_context);

        // Act - First seed execution
        await seeder.SeedAsync(CancellationToken.None);
        var countAfterFirstSeed = await _context.PayrollComponents.CountAsync();

        // Assert - Elements are created
        countAfterFirstSeed.Should().BeGreaterThan(0);

        // Act - Second seed execution (idempotency check)
        await seeder.SeedAsync(CancellationToken.None);
        var countAfterSecondSeed = await _context.PayrollComponents.CountAsync();

        // Assert - No duplicates created
        countAfterSecondSeed.Should().Be(countAfterFirstSeed);
    }

    [Fact]
    public async Task CreatePayrollComponent_HandlerAndPersistence_SavesAuditFields()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CreatePayrollComponentCommandHandler>>();
        var handler = new CreatePayrollComponentCommandHandler(
            _unitOfWork,
            _currentUserService,
            _dateTimeService,
            logger);

        var command = new CreatePayrollComponentCommand(
            CompanyId: 1,
            ComponentCode: "TRAVEL",
            ComponentName: "Travel Allowance",
            ComponentType: ComponentType.Allowance,
            CalculationMethod: CalculationMethod.Fixed,
            CalculationValue: 120.00m,
            Formula: null,
            IsTaxable: false,
            IsFnpfApplicable: true,
            DisplayOrder: 15,
            Description: "Travel expenses"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - Handler result
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);

        // Assert - Persistence
        var persisted = await _context.PayrollComponents.FirstOrDefaultAsync(x => x.Id == result.Value);
        persisted.Should().NotBeNull();
        persisted!.ComponentCode.Should().Be("TRAVEL");
        persisted.CalculationValue.Should().Be(120.00m);

        // Assert - Audit Interceptor fields
        persisted.CreatedBy.Should().Be("integration-test-user");
        persisted.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetPayrollComponentList_AppliesQueryFilterAndPaging()
    {
        // Arrange
        var seeder = new PayrollComponentSeeder(_context);
        await seeder.SeedAsync(CancellationToken.None); // Populates database

        var handler = new GetPayrollComponentListQueryHandler(_unitOfWork, _currentUserService);
        var query = new GetPayrollComponentListQuery(
            CompanyId: 1,
            SearchTerm: "Allowance",
            ComponentTypeFilter: null,
            ActiveOnly: true,
            PageNumber: 1,
            PageSize: 5);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Items.Should().NotBeEmpty();
        result.Value.Items.All(x => x.ComponentName.Contains("Allowance")).Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
