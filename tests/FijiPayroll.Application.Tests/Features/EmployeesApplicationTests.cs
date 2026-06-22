using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.Employees.Commands.TerminateEmployee;
using FijiPayroll.Application.Features.Employees.Queries.GetEmployeeDetail;
using FijiPayroll.Application.Features.Employees.Queries.GetEmployeesList;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Shared.Constants;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FijiPayroll.Application.Tests.Features;

/// <summary>
/// Unit tests for Employee feature queries and commands.
/// </summary>
public sealed class EmployeesApplicationTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<TerminateEmployeeCommandHandler> _terminateLogger;

    /// <summary>Sets up mocks and unit of work context.</summary>
    public EmployeesApplicationTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _employeeRepository = Substitute.For<IEmployeeRepository>();
        _unitOfWork.Employees.Returns(_employeeRepository);
        _currentUser = Substitute.For<ICurrentUserService>();
        _dateTime = Substitute.For<IDateTimeService>();
        _terminateLogger = Substitute.For<ILogger<TerminateEmployeeCommandHandler>>();
    }

    [Fact]
    public async Task GetEmployeesList_ShouldReturnFailure_WhenNoPermission()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.EmployeesView).Returns(false);
        var query = new GetEmployeesListQuery(1, null, null, 1, 10);
        var handler = new GetEmployeesListQueryHandler(_unitOfWork, _currentUser);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("permission");
    }

    [Fact]
    public async Task GetEmployeesList_ShouldReturnFailure_WhenNoCompanyAccess()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.EmployeesView).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(false);
        var query = new GetEmployeesListQuery(1, null, null, 1, 10);
        var handler = new GetEmployeesListQueryHandler(_unitOfWork, _currentUser);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("access");
    }

    [Fact]
    public async Task GetEmployeesList_ShouldReturnPagedResults_WhenAuthorized()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.EmployeesView).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);

        var employee = Employee.Create(1, "Test Employee", "TIN123", "FNPF123", "Resident", "Engineering", 2000, PayrollFrequencyType.Monthly, false, false, true);
        var list = new List<Employee> { employee };
        _employeeRepository.GetPagedAsync(1, "test", "Engineering", 1, 10, Arg.Any<CancellationToken>())
            .Returns((list, 1));

        var query = new GetEmployeesListQuery(1, "test", "Engineering", 1, 10);
        var handler = new GetEmployeesListQueryHandler(_unitOfWork, _currentUser);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].FullName.Should().Be("Test Employee");
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetEmployeeDetail_ShouldReturnFailure_WhenNoPermission()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.EmployeesView).Returns(false);
        var query = new GetEmployeeDetailQuery(1);
        var handler = new GetEmployeeDetailQueryHandler(_unitOfWork, _currentUser);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("permission");
    }

    [Fact]
    public async Task GetEmployeeDetail_ShouldReturnFailure_WhenEmployeeNotFound()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.EmployeesView).Returns(true);
        _employeeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var query = new GetEmployeeDetailQuery(1);
        var handler = new GetEmployeeDetailQueryHandler(_unitOfWork, _currentUser);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task GetEmployeeDetail_ShouldReturnFailure_WhenNoCompanyAccess()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.EmployeesView).Returns(true);
        var employee = Employee.Create(1, "Test Employee", "TIN123", "FNPF123", "Resident", "Engineering", 2000, PayrollFrequencyType.Monthly, false, false, true);
        _employeeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(employee);
        _currentUser.HasCompanyAccess(1).Returns(false);

        var query = new GetEmployeeDetailQuery(1);
        var handler = new GetEmployeeDetailQueryHandler(_unitOfWork, _currentUser);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("access");
    }

    [Fact]
    public async Task GetEmployeeDetail_ShouldReturnDetails_WhenAuthorized()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.EmployeesView).Returns(true);
        var employee = Employee.Create(1, "Test Employee", "TIN123", "FNPF123", "Resident", "Engineering", 2000, PayrollFrequencyType.Monthly, false, false, true);
        _employeeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(employee);
        _currentUser.HasCompanyAccess(1).Returns(true);

        var query = new GetEmployeeDetailQuery(1);
        var handler = new GetEmployeeDetailQueryHandler(_unitOfWork, _currentUser);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FullName.Should().Be("Test Employee");
        result.Value.ResidencyStatus.Should().Be("Resident");
    }

    [Fact]
    public async Task TerminateEmployee_ShouldThrowForbidden_WhenNoPermission()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.EmployeesTerminate).Returns(false);
        var command = new TerminateEmployeeCommand(1);
        var handler = new TerminateEmployeeCommandHandler(_unitOfWork, _currentUser, _dateTime, _terminateLogger);

        // Act & Assert
        await handler.Awaiting(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task TerminateEmployee_ShouldReturnFailure_WhenEmployeeNotFound()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.EmployeesTerminate).Returns(true);
        _employeeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var command = new TerminateEmployeeCommand(1);
        var handler = new TerminateEmployeeCommandHandler(_unitOfWork, _currentUser, _dateTime, _terminateLogger);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task TerminateEmployee_ShouldReturnFailure_WhenNoCompanyAccess()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.EmployeesTerminate).Returns(true);
        var employee = Employee.Create(1, "Test Employee", "TIN123", "FNPF123", "Resident", "Engineering", 2000, PayrollFrequencyType.Monthly, false, false, true);
        _employeeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(employee);
        _currentUser.HasCompanyAccess(1).Returns(false);

        var command = new TerminateEmployeeCommand(1);
        var handler = new TerminateEmployeeCommandHandler(_unitOfWork, _currentUser, _dateTime, _terminateLogger);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("access");
    }

    [Fact]
    public async Task TerminateEmployee_ShouldTerminateAndSave_WhenAuthorized()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.EmployeesTerminate).Returns(true);
        var employee = Employee.Create(1, "Test Employee", "TIN123", "FNPF123", "Resident", "Engineering", 2000, PayrollFrequencyType.Monthly, false, false, true);
        var paymentMethod = EmployeePaymentMethod.Create(PaymentMethodType.Cash, 100, true);
        employee.AddPaymentMethod(paymentMethod);

        _employeeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(employee);
        _currentUser.HasCompanyAccess(1).Returns(true);
        _currentUser.Username.Returns("admin");
        _dateTime.UtcNow.Returns(new DateTime(2026, 6, 22));

        var command = new TerminateEmployeeCommand(1);
        var handler = new TerminateEmployeeCommandHandler(_unitOfWork, _currentUser, _dateTime, _terminateLogger);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        employee.IsActive.Should().BeFalse();
        employee.ModifiedBy.Should().Be("admin");
        employee.ModifiedAt.Should().Be(new DateTime(2026, 6, 22));
        paymentMethod.IsPrimary.Should().BeFalse();
        paymentMethod.Percentage.Should().Be(0);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
