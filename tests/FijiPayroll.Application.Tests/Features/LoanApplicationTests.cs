using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.Loans.Commands.CreateLoan;
using FijiPayroll.Application.Features.Loans.Commands.SuspendLoan;
using FijiPayroll.Application.Features.Loans.Commands.ResumeLoan;
using FijiPayroll.Application.Features.Loans.Commands.WriteOffLoan;
using FijiPayroll.Application.Features.Loans.Queries.GetEmployeeLoans;
using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Entities.Payroll;
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
/// Unit tests for Loan Management feature commands and queries.
/// </summary>
public sealed class LoanApplicationTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoanRepository _loanRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<CreateLoanCommandHandler> _createLogger;
    private readonly ILogger<SuspendLoanCommandHandler> _suspendLogger;
    private readonly ILogger<ResumeLoanCommandHandler> _resumeLogger;
    private readonly ILogger<WriteOffLoanCommandHandler> _writeOffLogger;

    /// <summary>Sets up mocks and unit of work context.</summary>
    public LoanApplicationTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _loanRepository = Substitute.For<ILoanRepository>();
        _employeeRepository = Substitute.For<IEmployeeRepository>();
        _unitOfWork.Loans.Returns(_loanRepository);
        _unitOfWork.Employees.Returns(_employeeRepository);
        _currentUser = Substitute.For<ICurrentUserService>();
        _dateTime = Substitute.For<IDateTimeService>();
        _createLogger = Substitute.For<ILogger<CreateLoanCommandHandler>>();
        _suspendLogger = Substitute.For<ILogger<SuspendLoanCommandHandler>>();
        _resumeLogger = Substitute.For<ILogger<ResumeLoanCommandHandler>>();
        _writeOffLogger = Substitute.For<ILogger<WriteOffLoanCommandHandler>>();
    }

    private static void SetId<T>(T entity, int id) where T : BaseEntity
    {
        var prop = typeof(BaseEntity).GetProperty("Id");
        prop?.SetValue(entity, id);
    }

    [Fact]
    public async Task CreateLoan_ShouldThrowForbidden_WhenNoPermission()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.LoansCreate).Returns(false);
        var command = new CreateLoanCommand(1, 1, "Salary Advance", 1000m, 0.05m, 100m, DateTime.UtcNow);
        var handler = new CreateLoanCommandHandler(_unitOfWork, _currentUser, _dateTime, _createLogger);

        // Act & Assert
        await handler.Awaiting(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task CreateLoan_ShouldReturnFailure_WhenNoCompanyAccess()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.LoansCreate).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(false);
        var command = new CreateLoanCommand(1, 1, "Salary Advance", 1000m, 0.05m, 100m, DateTime.UtcNow);
        var handler = new CreateLoanCommandHandler(_unitOfWork, _currentUser, _dateTime, _createLogger);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("access");
    }

    [Fact]
    public async Task CreateLoan_ShouldReturnFailure_WhenEmployeeNotFound()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.LoansCreate).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);
        _employeeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var command = new CreateLoanCommand(1, 1, "Salary Advance", 1000m, 0.05m, 100m, DateTime.UtcNow);
        var handler = new CreateLoanCommandHandler(_unitOfWork, _currentUser, _dateTime, _createLogger);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task CreateLoan_ShouldPersist_WhenValidRequest()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.LoansCreate).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);
        _currentUser.Username.Returns("test-user");
        _dateTime.UtcNow.Returns(DateTime.UtcNow);

        var employee = Employee.Create(1, "John Doe", "TIN123", "FNPF123", "Resident", "IT", 3000m, PayrollFrequencyType.Fortnightly, false, false, true);
        SetId(employee, 10);
        _employeeRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(employee);

        var command = new CreateLoanCommand(1, 10, "Salary Advance", 1000m, 0.05m, 100m, DateTime.UtcNow);
        var handler = new CreateLoanCommandHandler(_unitOfWork, _currentUser, _dateTime, _createLogger);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _loanRepository.Received(1).AddAsync(Arg.Any<Loan>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SuspendLoan_ShouldUpdateStatus()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.LoansManage).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);
        _currentUser.Username.Returns("test-user");

        var loan = Loan.Create(1, 10, "Salary Advance", 1000m, 0.05m, 100m, DateTime.UtcNow);
        SetId(loan, 5);
        _loanRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(loan);

        var command = new SuspendLoanCommand(1, 5);
        var handler = new SuspendLoanCommandHandler(_unitOfWork, _currentUser, _dateTime, _suspendLogger);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        loan.Status.Should().Be(LoanStatus.Suspended);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResumeLoan_ShouldUpdateStatus()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.LoansManage).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);
        _currentUser.Username.Returns("test-user");

        var loan = Loan.Create(1, 10, "Salary Advance", 1000m, 0.05m, 100m, DateTime.UtcNow);
        loan.Suspend();
        SetId(loan, 5);
        _loanRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(loan);

        var command = new ResumeLoanCommand(1, 5);
        var handler = new ResumeLoanCommandHandler(_unitOfWork, _currentUser, _dateTime, _resumeLogger);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        loan.Status.Should().Be(LoanStatus.Active);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WriteOffLoan_ShouldSetBalanceToZero()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.LoansManage).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);
        _currentUser.Username.Returns("test-user");

        var loan = Loan.Create(1, 10, "Salary Advance", 1000m, 0.05m, 100m, DateTime.UtcNow);
        SetId(loan, 5);
        _loanRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(loan);

        var command = new WriteOffLoanCommand(1, 5);
        var handler = new WriteOffLoanCommandHandler(_unitOfWork, _currentUser, _dateTime, _writeOffLogger);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        loan.Status.Should().Be(LoanStatus.WrittenOff);
        loan.RemainingBalance.Should().Be(0m);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetEmployeeLoans_ShouldReturnList()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.LoansView).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);

        var employee = Employee.Create(1, "John Doe", "TIN123", "FNPF123", "Resident", "IT", 3000m, PayrollFrequencyType.Fortnightly, false, false, true);
        SetId(employee, 10);
        _employeeRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(employee);

        var loan1 = Loan.Create(1, 10, "Salary Advance", 1000m, 0.05m, 100m, DateTime.UtcNow);
        SetId(loan1, 5);
        var list = new List<Loan> { loan1 };
        _loanRepository.GetLoansByEmployeeAsync(10, Arg.Any<CancellationToken>()).Returns(list);

        var query = new GetEmployeeLoansQuery(1, 10);
        var handler = new GetEmployeeLoansQueryHandler(_unitOfWork, _currentUser);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].LoanDescription.Should().Be("Salary Advance");
    }
}
