using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Features.Employees.Commands.CreateEmployee;
using FijiPayroll.Application.Features.Employees.Commands.UpdateEmployee;
using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Events;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using FijiPayroll.Persistence.Interceptors;
using FijiPayroll.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FijiPayroll.Integration.Tests;

public sealed class AuditIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICorrelationContext _correlationContext;
    private readonly IEventBus _eventBus;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmployeeRepository _repository;
    private readonly Guid _testCorrelationId = Guid.NewGuid();

    public AuditIntegrationTests()
    {
        _currentUserAccessor = Substitute.For<ICurrentUserAccessor>();
        _currentUserAccessor.Username.Returns("audit-test-user");

        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.Username.Returns("audit-test-user");
        _currentUserService.HasPermission(Arg.Any<string>()).Returns(true);
        _currentUserService.HasCompanyAccess(Arg.Any<int>()).Returns(true);

        _dateTimeService = Substitute.For<IDateTimeService>();
        _dateTimeService.UtcNow.Returns(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        _tenantProvider = Substitute.For<ITenantProvider>();
        _tenantProvider.GetCurrentCompanyId().Returns(1);

        _correlationContext = Substitute.For<ICorrelationContext>();
        _correlationContext.CorrelationId.Returns(_testCorrelationId);

        _eventBus = Substitute.For<IEventBus>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var auditableInterceptor = new AuditableEntityInterceptor(_currentUserAccessor);
        var auditLogInterceptor = new AuditLogInterceptor(_correlationContext, _currentUserAccessor, _tenantProvider, _eventBus);

        _context = new ApplicationDbContext(options, auditableInterceptor, auditLogInterceptor, _tenantProvider);

        _repository = new EmployeeRepository(_context);
        var mockCompRepo = Substitute.For<IPayrollComponentRepository>();
        var mockRunRepo = Substitute.For<IPayrollRunRepository>();
        var mockTaxRepo = Substitute.For<ITaxBracketRepository>();
        var mockLookupRepo = Substitute.For<IMasterLookupRepository>();
        _unitOfWork = new UnitOfWork(_context, mockCompRepo, mockRunRepo, _repository, mockTaxRepo, mockLookupRepo);
    }

    [Fact]
    public async Task CreateEmployee_ShouldGenerateAuditLogAndOutboxEvent()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CreateEmployeeCommandHandler>>();
        var handler = new CreateEmployeeCommandHandler(_unitOfWork, _currentUserService, _dateTimeService, logger);

        var paymentMethods = new List<PaymentMethodInput>
        {
            new(PaymentMethodType.Cash, 100m, true)
        };

        var command = new CreateEmployeeCommand(
            CompanyId: 1,
            FullName: "Audit Test Employee",
            Tin: "123456789",
            FnpfNumber: "98765-A",
            ResidencyStatus: "Resident",
            Department: "QA",
            BaseSalary: 2000m,
            Frequency: PayrollFrequency.Fortnightly,
            IsFnpfExempt: false,
            IsTaxExempt: false,
            IsActive: true,
            EmploymentType: EmploymentType.Permanent,
            Branch: "Suva",
            Position: "QA Engineer",
            Email: "audit@company.com",
            PaymentMethods: paymentMethods
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var employeeId = result.Value;

        // Verify Audit Log entry in DB
        var auditLog = await _context.AuditLogs.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.EntityName == "Employee");
        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be("Create");
        auditLog.UserId.Should().Be("audit-test-user");
        auditLog.CompanyId.Should().Be(1);
        auditLog.CorrelationId.Should().Be(_testCorrelationId);
        auditLog.EntityId.Should().Be(employeeId.ToString());
        auditLog.Changes.Should().Contain("Audit Test Employee");

        // Verify Event Outbox entry in DB
        var outboxEvent = await _context.EntityEvents.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.EventType == "EmployeeCreatedEvent");
        outboxEvent.Should().NotBeNull();
        outboxEvent!.CompanyId.Should().Be(1);
        outboxEvent.CorrelationId.Should().Be(_testCorrelationId);
        outboxEvent.Payload.Should().Contain("Audit Test Employee");

        // Verify that event was published to the EventBus
        await _eventBus.Received(1).PublishAsync(Arg.Is<EmployeeCreatedEvent>(e => e.EmployeeId == employeeId && e.FullName == "Audit Test Employee"));
    }

    [Fact]
    public async Task UpdateEmployee_ShouldLogOnlyChangedFields()
    {
        // Arrange
        var employee = Employee.Create(1, "Before Update", "123456789", "98765-A", "Resident", "QA", 2000m, PayrollFrequency.Fortnightly, false, false, true, EmploymentType.Permanent, "Suva", "QA Engineer", "audit@company.com");
        await _context.Employees.AddAsync(employee);
        await _context.SaveChangesAsync();

        // Clear tracker and clear mock calls so we only audit the update
        _context.ChangeTracker.Clear();
        _eventBus.ClearReceivedCalls();

        var logger = Substitute.For<ILogger<UpdateEmployeeCommandHandler>>();
        var handler = new UpdateEmployeeCommandHandler(_unitOfWork, _currentUserService, _dateTimeService, logger);

        var paymentMethods = new List<PaymentMethodInput>
        {
            new(PaymentMethodType.Cash, 100m, true)
        };

        var command = new UpdateEmployeeCommand(
            Id: employee.Id,
            CompanyId: 1,
            FullName: "After Update", // Changed name
            Tin: "123456789",          // Unchanged
            FnpfNumber: "98765-A",       // Unchanged
            ResidencyStatus: "Resident", // Unchanged
            Department: "QA",          // Unchanged
            BaseSalary: 2500m,         // Changed salary
            Frequency: PayrollFrequency.Fortnightly,
            IsFnpfExempt: false,
            IsTaxExempt: false,
            IsActive: true,
            EmploymentType: EmploymentType.Permanent,
            Branch: "Suva",
            Position: "QA Engineer",
            Email: "audit@company.com",
            PaymentMethods: paymentMethods
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updateLogs = await _context.AuditLogs.IgnoreQueryFilters().Where(x => x.EntityName == "Employee" && x.Action == "Update").ToListAsync();
        updateLogs.Should().NotBeEmpty();
        var updateLog = updateLogs.Last();
        updateLog.Changes.Should().Contain("After Update");
        updateLog.Changes.Should().Contain("2500");
        updateLog.Changes.Should().NotContain("QA Engineer"); // Unchanged property should not be present
    }

    [Fact]
    public async Task DeactivateEmployee_ShouldRaiseEmployeeTerminatedEvent()
    {
        // Arrange
        var employee = Employee.Create(1, "Terminated Employee", "123456789", "98765-A", "Resident", "QA", 2000m, PayrollFrequency.Fortnightly, false, false, true, EmploymentType.Permanent, "Suva", "QA Engineer", "audit@company.com");
        await _context.Employees.AddAsync(employee);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();
        _eventBus.ClearReceivedCalls();

        var logger = Substitute.For<ILogger<UpdateEmployeeCommandHandler>>();
        var handler = new UpdateEmployeeCommandHandler(_unitOfWork, _currentUserService, _dateTimeService, logger);

        var paymentMethods = new List<PaymentMethodInput>
        {
            new(PaymentMethodType.Cash, 100m, true)
        };

        var command = new UpdateEmployeeCommand(
            Id: employee.Id,
            CompanyId: 1,
            FullName: "Terminated Employee",
            Tin: "123456789",
            FnpfNumber: "98765-A",
            ResidencyStatus: "Resident",
            Department: "QA",
            BaseSalary: 2000m,
            Frequency: PayrollFrequency.Fortnightly,
            IsFnpfExempt: false,
            IsTaxExempt: false,
            IsActive: false, // Set to inactive (termination)
            EmploymentType: EmploymentType.Permanent,
            Branch: "Suva",
            Position: "QA Engineer",
            Email: "audit@company.com",
            PaymentMethods: paymentMethods
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify Event Outbox entry in DB
        var outboxEvent = await _context.EntityEvents.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.EventType == "EmployeeTerminatedEvent");
        outboxEvent.Should().NotBeNull();
        outboxEvent!.CompanyId.Should().Be(1);

        // Verify event published to the EventBus
        await _eventBus.Received(1).PublishAsync(Arg.Is<EmployeeTerminatedEvent>(e => e.EmployeeId == employee.Id && e.TerminatedBy == "audit-test-user"));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
