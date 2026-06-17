using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.Employees.Commands.CreateEmployee;
using FijiPayroll.Application.Features.Employees.Commands.UpdateEmployee;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
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

public sealed class EmployeeIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmployeeRepository _repository;

    public EmployeeIntegrationTests()
    {
        _currentUserAccessor = Substitute.For<ICurrentUserAccessor>();
        _currentUserAccessor.Username.Returns("integration-test-user");

        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.Username.Returns("integration-test-user");
        _currentUserService.HasPermission(Arg.Any<string>()).Returns(true);
        _currentUserService.HasCompanyAccess(Arg.Any<int>()).Returns(true);

        _dateTimeService = Substitute.For<IDateTimeService>();
        _dateTimeService.UtcNow.Returns(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentCompanyId().Returns(1);

        var interceptor = new AuditableEntityInterceptor(_currentUserAccessor);
        _context = new ApplicationDbContext(options, interceptor, tenantProvider);

        _repository = new EmployeeRepository(_context);
        var mockCompRepo = Substitute.For<IPayrollComponentRepository>();
        var mockRunRepo = Substitute.For<IPayrollRunRepository>();
        var mockTaxRepo = Substitute.For<ITaxBracketRepository>();
        var mockLookupRepo = Substitute.For<IMasterLookupRepository>();
        _unitOfWork = new UnitOfWork(_context, mockCompRepo, mockRunRepo, _repository, mockTaxRepo, mockLookupRepo);
    }

    [Fact]
    public async Task CreateEmployee_ValidRequest_SavesToDatabaseAndCalculatesQualityScore()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CreateEmployeeCommandHandler>>();
        var handler = new CreateEmployeeCommandHandler(_unitOfWork, _currentUserService, _dateTimeService, logger);
        
        var paymentMethods = new List<PaymentMethodInput>
        {
            new(PaymentMethodType.BankTransfer, 100m, true, "BSP", "123456789", "BSP-FJ")
        };

        var command = new CreateEmployeeCommand(
            CompanyId: 1,
            FullName: "Samisoni Fiji",
            Tin: "998877665",
            FnpfNumber: "12345-F",
            ResidencyStatus: "Resident",
            Department: "HR",
            BaseSalary: 2500m,
            Frequency: PayrollFrequencyType.Fortnightly,
            IsFnpfExempt: false,
            IsTaxExempt: false,
            IsActive: true,
            EmploymentType: EmploymentType.Permanent,
            Branch: "Suva",
            Position: "HR Officer",
            Email: "samisoni@company.com",
            PaymentMethods: paymentMethods
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);

        var emp = await _context.Employees
            .Include(e => e.PaymentMethods)
            .FirstOrDefaultAsync(e => e.Id == result.Value);
        
        emp.Should().NotBeNull();
        emp!.FullName.Should().Be("Samisoni Fiji");
        emp.EmploymentType.Should().Be(EmploymentType.Permanent);
        emp.Branch.Should().Be("Suva");
        emp.Position.Should().Be("HR Officer");
        emp.Email.Should().Be("samisoni@company.com");
        emp.DataQualityScore.Should().Be(100.0); // All 6 components satisfied (Name, TIN, FNPF, Dept/Branch/Pos, primary payment method, Email)
        emp.PaymentMethods.Should().HaveCount(1);
        emp.PaymentMethods.First().BankName.Should().Be("BSP");
    }

    [Fact]
    public async Task UpdateEmployee_ValidRequest_ModifiesPropertiesAndUpdatesMethods()
    {
        // Arrange
        var emp = Employee.Create(1, "Original Name", "998877665", "12345-F", "Resident", "Finance", 2000m, PayrollFrequencyType.Fortnightly, false, false, true, EmploymentType.Permanent, "Suva", "Officer", "original@company.com");
        emp.AddPaymentMethod(EmployeePaymentMethod.Create(PaymentMethodType.Cash, 100m, true));
        await _context.Employees.AddAsync(emp);
        await _context.SaveChangesAsync();

        var logger = Substitute.For<ILogger<UpdateEmployeeCommandHandler>>();
        var handler = new UpdateEmployeeCommandHandler(_unitOfWork, _currentUserService, _dateTimeService, logger);

        var updatedPaymentMethods = new List<PaymentMethodInput>
        {
            new(PaymentMethodType.BankTransfer, 100m, true, "Westpac", "987654321", "WPC-FJ")
        };

        var command = new UpdateEmployeeCommand(
            Id: emp.Id,
            CompanyId: 1,
            FullName: "Updated Name",
            Tin: "998877665",
            FnpfNumber: "12345-F",
            ResidencyStatus: "Resident",
            Department: "HR",
            BaseSalary: 3000m,
            Frequency: PayrollFrequencyType.Fortnightly,
            IsFnpfExempt: false,
            IsTaxExempt: false,
            IsActive: true,
            EmploymentType: EmploymentType.Contract,
            Branch: "Nadi",
            Position: "Manager",
            Email: "manager@company.com",
            PaymentMethods: updatedPaymentMethods
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updated = await _context.Employees
            .Include(e => e.PaymentMethods)
            .FirstOrDefaultAsync(e => e.Id == emp.Id);

        updated!.FullName.Should().Be("Updated Name");
        updated.EmploymentType.Should().Be(EmploymentType.Contract);
        updated.Branch.Should().Be("Nadi");
        updated.Position.Should().Be("Manager");
        updated.Email.Should().Be("manager@company.com");
        updated.DataQualityScore.Should().Be(100.0);
        updated.PaymentMethods.Should().HaveCount(1);
        updated.PaymentMethods.First().MethodType.Should().Be(PaymentMethodType.BankTransfer);
        updated.PaymentMethods.First().BankName.Should().Be("Westpac");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
