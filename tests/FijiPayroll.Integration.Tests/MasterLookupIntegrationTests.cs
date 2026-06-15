using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.Lookups.Commands.ArchiveLookup;
using FijiPayroll.Application.Features.Lookups.Commands.CreateLookup;
using FijiPayroll.Application.Features.Lookups.Commands.UpdateLookup;
using FijiPayroll.Application.Features.Lookups.Queries.GetLookups;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using FijiPayroll.Persistence.Interceptors;
using FijiPayroll.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FijiPayroll.Integration.Tests;

public sealed class MasterLookupIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMasterLookupRepository _repository;
    private readonly IReferenceDataCache _cache;

    public MasterLookupIntegrationTests()
    {
        _currentUserAccessor = Substitute.For<ICurrentUserAccessor>();
        _currentUserAccessor.Username.Returns("integration-test-user");

        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.Username.Returns("integration-test-user");
        _currentUserService.HasPermission(Arg.Any<string>()).Returns(true);
        _currentUserService.HasCompanyAccess(Arg.Any<int>()).Returns(true);

        _dateTimeService = Substitute.For<IDateTimeService>();
        _dateTimeService.UtcNow.Returns(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        _cache = Substitute.For<IReferenceDataCache>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentCompanyId().Returns(1);

        var interceptor = new AuditableEntityInterceptor(_currentUserAccessor);
        _context = new ApplicationDbContext(options, interceptor, tenantProvider);

        _repository = new MasterLookupRepository(_context);
        var mockCompRepo = Substitute.For<IPayrollComponentRepository>();
        var mockRunRepo = Substitute.For<IPayrollRunRepository>();
        var mockEmpRepo = Substitute.For<IEmployeeRepository>();
        var mockTaxRepo = Substitute.For<ITaxBracketRepository>();
        _unitOfWork = new UnitOfWork(_context, mockCompRepo, mockRunRepo, mockEmpRepo, mockTaxRepo, _repository);
    }

    [Fact]
    public async Task CreateLookup_ValidRequest_SavesToDatabaseAndInvalidatesCache()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CreateLookupCommandHandler>>();
        var handler = new CreateLookupCommandHandler(_unitOfWork, _currentUserService, _cache, _dateTimeService, logger);
        var command = new CreateLookupCommand(
            CompanyId: 1,
            Category: "DEPARTMENTS",
            Code: "HR",
            Name: "Human Resources",
            EffectiveFrom: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo: new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);

        var lookup = await _context.MasterLookups.FirstOrDefaultAsync(ml => ml.Id == result.Value);
        lookup.Should().NotBeNull();
        lookup!.Code.Should().Be("HR");
        lookup.Category.Should().Be("DEPARTMENTS");
        lookup.CreatedBy.Should().Be("integration-test-user");

        _cache.Received(1).InvalidateCategory("DEPARTMENTS");
    }

    [Fact]
    public async Task CreateLookup_DuplicateCode_ReturnsFailure()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CreateLookupCommandHandler>>();
        var handler = new CreateLookupCommandHandler(_unitOfWork, _currentUserService, _cache, _dateTimeService, logger);
        var command = new CreateLookupCommand(
            CompanyId: 1,
            Category: "DEPARTMENTS",
            Code: "HR",
            Name: "Human Resources",
            EffectiveFrom: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo: new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc)
        );

        // Act
        await handler.Handle(command, CancellationToken.None);
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task UpdateLookup_ValidRequest_ModifiesPropertiesAndInvalidatesCache()
    {
        // Arrange
        var existing = MasterLookup.Create(1, "DEPARTMENTS", "IT", "Information Tech", DateTime.UtcNow, DateTime.UtcNow.AddDays(10));
        await _context.MasterLookups.AddAsync(existing);
        await _context.SaveChangesAsync();

        var logger = Substitute.For<ILogger<UpdateLookupCommandHandler>>();
        var handler = new UpdateLookupCommandHandler(_unitOfWork, _currentUserService, _cache, _dateTimeService, logger);
        var command = new UpdateLookupCommand(
            Id: existing.Id,
            CompanyId: 1,
            Name: "IT Services",
            EffectiveFrom: existing.EffectiveFrom,
            EffectiveTo: existing.EffectiveTo,
            IsActive: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var updated = await _context.MasterLookups.FirstOrDefaultAsync(ml => ml.Id == existing.Id);
        updated!.Name.Should().Be("IT Services");
        updated.ModifiedBy.Should().Be("integration-test-user");

        _cache.Received(1).InvalidateCategory("DEPARTMENTS");
    }

    [Fact]
    public async Task ArchiveLookup_ValidRequest_SetsArchivedStatusAndReason()
    {
        // Arrange
        var existing = MasterLookup.Create(1, "DEPARTMENTS", "MKTG", "Marketing", DateTime.UtcNow, DateTime.UtcNow.AddDays(10));
        await _context.MasterLookups.AddAsync(existing);
        await _context.SaveChangesAsync();

        var logger = Substitute.For<ILogger<ArchiveLookupCommandHandler>>();
        var handler = new ArchiveLookupCommandHandler(_unitOfWork, _currentUserService, _cache, logger);
        var command = new ArchiveLookupCommand(existing.Id, 1, "No longer needed");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var archived = await _context.MasterLookups.IgnoreQueryFilters().FirstOrDefaultAsync(ml => ml.Id == existing.Id);
        archived!.IsArchived.Should().BeTrue();
        archived.ArchiveReason.Should().Be("No longer needed");
        archived.ArchivedBy.Should().Be("integration-test-user");

        _cache.Received(1).InvalidateCategory("DEPARTMENTS");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
