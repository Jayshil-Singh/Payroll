using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.Leave.Commands.ApproveLeaveRequest;
using FijiPayroll.Application.Features.Leave.Commands.RejectLeaveRequest;
using FijiPayroll.Application.Features.Leave.Commands.SubmitLeaveRequest;
using FijiPayroll.Application.Features.Leave.Queries.GetLeaveBalances;
using FijiPayroll.Application.Features.Leave.Queries.GetLeaveRequests;
using FijiPayroll.Application.Features.Leave.Queries.GetLeaveTypes;
using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Entities.Leave;
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
/// Unit tests for Leave Management feature commands and queries.
/// </summary>
public sealed class LeaveApplicationTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILeaveRepository _leaveRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<SubmitLeaveRequestCommandHandler> _submitLogger;
    private readonly ILogger<ApproveLeaveRequestCommandHandler> _approveLogger;
    private readonly ILogger<RejectLeaveRequestCommandHandler> _rejectLogger;

    /// <summary>Sets up mocks and unit of work context.</summary>
    public LeaveApplicationTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _leaveRepository = Substitute.For<ILeaveRepository>();
        _employeeRepository = Substitute.For<IEmployeeRepository>();
        _unitOfWork.Leave.Returns(_leaveRepository);
        _unitOfWork.Employees.Returns(_employeeRepository);
        _currentUser = Substitute.For<ICurrentUserService>();
        _dateTime = Substitute.For<IDateTimeService>();
        _submitLogger = Substitute.For<ILogger<SubmitLeaveRequestCommandHandler>>();
        _approveLogger = Substitute.For<ILogger<ApproveLeaveRequestCommandHandler>>();
        _rejectLogger = Substitute.For<ILogger<RejectLeaveRequestCommandHandler>>();
    }

    private static void SetId<T>(T entity, int id) where T : BaseEntity
    {
        var prop = typeof(BaseEntity).GetProperty("Id");
        prop?.SetValue(entity, id);
    }

    [Fact]
    public async Task SubmitLeaveRequest_ShouldThrowForbidden_WhenNoPermission()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.LeaveCreate).Returns(false);
        var command = new SubmitLeaveRequestCommand(1, 1, 1, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5), 4);
        var handler = new SubmitLeaveRequestCommandHandler(_unitOfWork, _currentUser, _dateTime, _submitLogger);

        // Act & Assert
        await handler.Awaiting(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task SubmitLeaveRequest_ShouldReturnFailure_WhenNoCompanyAccess()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.LeaveCreate).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(false);
        var command = new SubmitLeaveRequestCommand(1, 1, 1, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5), 4);
        var handler = new SubmitLeaveRequestCommandHandler(_unitOfWork, _currentUser, _dateTime, _submitLogger);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("access");
    }

    [Fact]
    public async Task SubmitLeaveRequest_ShouldReturnFailure_WhenEmployeeNotFound()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.LeaveCreate).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);
        _employeeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var command = new SubmitLeaveRequestCommand(1, 1, 1, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5), 4);
        var handler = new SubmitLeaveRequestCommandHandler(_unitOfWork, _currentUser, _dateTime, _submitLogger);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Employee with ID 1 was not found");
    }

    [Fact]
    public async Task SubmitLeaveRequest_ShouldReturnFailure_WhenLeaveTypeNotFound()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.LeaveCreate).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);
        var employee = Employee.Create(1, "Test Employee", "TIN123", "FNPF123", "Resident", "Engineering", 2000, PayrollFrequencyType.Monthly, false, false, true);
        SetId(employee, 1);
        _employeeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(employee);
        _leaveRepository.GetTypeByIdAsync(1, Arg.Any<CancellationToken>()).Returns((LeaveType?)null);

        var command = new SubmitLeaveRequestCommand(1, 1, 1, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5), 4);
        var handler = new SubmitLeaveRequestCommandHandler(_unitOfWork, _currentUser, _dateTime, _submitLogger);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Leave type with ID 1 was not found");
    }

    [Fact]
    public async Task SubmitLeaveRequest_ShouldSubmitAndSave_WhenAuthorizedAndValid()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.LeaveCreate).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);
        _currentUser.Username.Returns("test_user");
        _dateTime.UtcNow.Returns(new DateTime(2026, 6, 22));

        var employee = Employee.Create(1, "Test Employee", "TIN123", "FNPF123", "Resident", "Engineering", 2000, PayrollFrequencyType.Monthly, false, false, true);
        SetId(employee, 1);
        _employeeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(employee);

        var leaveType = LeaveType.Create(1, "Annual Leave", LeaveCategory.AnnualLeave, 10, true, true);
        SetId(leaveType, 1);
        _leaveRepository.GetTypeByIdAsync(1, Arg.Any<CancellationToken>()).Returns(leaveType);

        var startDate = new DateTime(2026, 7, 1);
        var endDate = new DateTime(2026, 7, 5);
        var command = new SubmitLeaveRequestCommand(1, 1, 1, startDate, endDate, 4, "Notes");
        var handler = new SubmitLeaveRequestCommandHandler(_unitOfWork, _currentUser, _dateTime, _submitLogger);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _leaveRepository.Received(1).AddRequestAsync(Arg.Any<LeaveRequest>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveLeaveRequest_ShouldThrowForbidden_WhenNoPermission()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.LeaveApprove).Returns(false);
        var command = new ApproveLeaveRequestCommand(1);
        var handler = new ApproveLeaveRequestCommandHandler(_unitOfWork, _currentUser, _dateTime, _approveLogger);

        // Act & Assert
        await handler.Awaiting(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task ApproveLeaveRequest_ShouldReturnFailure_WhenRequestNotFound()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.LeaveApprove).Returns(true);
        _leaveRepository.GetRequestByIdAsync(1, Arg.Any<CancellationToken>()).Returns((LeaveRequest?)null);
        var command = new ApproveLeaveRequestCommand(1);
        var handler = new ApproveLeaveRequestCommandHandler(_unitOfWork, _currentUser, _dateTime, _approveLogger);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Leave request with ID 1 was not found");
    }

    [Fact]
    public async Task ApproveLeaveRequest_ShouldApproveAndReserve_WhenAuthorizedAndValid()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.LeaveApprove).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);
        _currentUser.Username.Returns("approver_user");
        _dateTime.UtcNow.Returns(new DateTime(2026, 6, 22));

        var leaveType = LeaveType.Create(1, "Annual Leave", LeaveCategory.AnnualLeave, 10, true, true);
        SetId(leaveType, 1);
        var leaveRequest = LeaveRequest.Submit(1, 1, 1, new DateTime(2026, 7, 1), new DateTime(2026, 7, 5), 4, true, "Notes");
        SetId(leaveRequest, 1);
        
        _leaveRepository.GetRequestByIdAsync(1, Arg.Any<CancellationToken>()).Returns(leaveRequest);
        _leaveRepository.GetTypeByIdAsync(1, Arg.Any<CancellationToken>()).Returns(leaveType);

        var leaveBalance = LeaveBalance.Initialise(1, 1, 1, 2026, 10);
        SetId(leaveBalance, 1);
        _leaveRepository.GetBalanceAsync(1, 1, 2026, Arg.Any<CancellationToken>()).Returns(leaveBalance);

        var command = new ApproveLeaveRequestCommand(1);
        var handler = new ApproveLeaveRequestCommandHandler(_unitOfWork, _currentUser, _dateTime, _approveLogger);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        leaveRequest.Status.Should().Be(LeaveStatus.Approved);
        leaveBalance.Pending.Should().Be(4);
        leaveBalance.Available.Should().Be(6);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveLeaveRequest_ShouldReturnFailure_WhenInsufficientBalance()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.LeaveApprove).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);
        _currentUser.Username.Returns("approver_user");

        var leaveType = LeaveType.Create(1, "Annual Leave", LeaveCategory.AnnualLeave, 10, true, true);
        SetId(leaveType, 1);
        var leaveRequest = LeaveRequest.Submit(1, 1, 1, new DateTime(2026, 7, 1), new DateTime(2026, 7, 15), 12, true, "Notes");
        SetId(leaveRequest, 1);
        
        _leaveRepository.GetRequestByIdAsync(1, Arg.Any<CancellationToken>()).Returns(leaveRequest);
        _leaveRepository.GetTypeByIdAsync(1, Arg.Any<CancellationToken>()).Returns(leaveType);

        var leaveBalance = LeaveBalance.Initialise(1, 1, 1, 2026, 10);
        SetId(leaveBalance, 1);
        _leaveRepository.GetBalanceAsync(1, 1, 2026, Arg.Any<CancellationToken>()).Returns(leaveBalance);

        var command = new ApproveLeaveRequestCommand(1);
        var handler = new ApproveLeaveRequestCommandHandler(_unitOfWork, _currentUser, _dateTime, _approveLogger);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Insufficient leave balance");
        leaveRequest.Status.Should().Be(LeaveStatus.Pending); // Stays pending
    }

    [Fact]
    public async Task RejectLeaveRequest_ShouldRejectRequest_WhenAuthorized()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.LeaveApprove).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);
        _currentUser.Username.Returns("rejecter_user");
        _dateTime.UtcNow.Returns(new DateTime(2026, 6, 22));

        var leaveRequest = LeaveRequest.Submit(1, 1, 1, new DateTime(2026, 7, 1), new DateTime(2026, 7, 5), 4, true, "Notes");
        SetId(leaveRequest, 1);
        _leaveRepository.GetRequestByIdAsync(1, Arg.Any<CancellationToken>()).Returns(leaveRequest);

        var command = new RejectLeaveRequestCommand(1, "Too busy at work");
        var handler = new RejectLeaveRequestCommandHandler(_unitOfWork, _currentUser, _dateTime, _rejectLogger);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        leaveRequest.Status.Should().Be(LeaveStatus.Rejected);
        leaveRequest.RejectionReason.Should().Be("Too busy at work");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetLeaveRequests_ShouldReturnRequests_WhenAuthorized()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.LeaveView).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);

        var leaveType = LeaveType.Create(1, "Annual Leave", LeaveCategory.AnnualLeave, 10, true, true);
        SetId(leaveType, 1);
        var leaveRequest = LeaveRequest.Submit(1, 1, 1, new DateTime(2026, 7, 1), new DateTime(2026, 7, 5), 4, true, "Notes");
        SetId(leaveRequest, 1);
        // Reflecting leave type via reflection since internal setter/backing field
        var typeProp = typeof(LeaveRequest).GetProperty("LeaveType");
        typeProp?.SetValue(leaveRequest, leaveType);

        var list = new List<LeaveRequest> { leaveRequest };
        _leaveRepository.GetRequestsByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(list);

        var employee = Employee.Create(1, "Test Employee", "TIN123", "FNPF123", "Resident", "Engineering", 2000, PayrollFrequencyType.Monthly, false, false, true);
        SetId(employee, 1);
        _employeeRepository.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Employee>>(new List<Employee> { employee }));

        var query = new GetLeaveRequestsQuery(1, null);
        var handler = new GetLeaveRequestsQueryHandler(_unitOfWork, _currentUser);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].EmployeeName.Should().Be("Test Employee");
        result.Value[0].LeaveTypeName.Should().Be("Annual Leave");
    }

    [Fact]
    public async Task GetLeaveBalances_ShouldReturnBalances_WhenAuthorized()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.LeaveView).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);

        var employee = Employee.Create(1, "Test Employee", "TIN123", "FNPF123", "Resident", "Engineering", 2000, PayrollFrequencyType.Monthly, false, false, true);
        SetId(employee, 1);
        _employeeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(employee);

        var leaveType = LeaveType.Create(1, "Annual Leave", LeaveCategory.AnnualLeave, 10, true, true);
        SetId(leaveType, 1);
        var leaveBalance = LeaveBalance.Initialise(1, 1, 1, 2026, 10);
        SetId(leaveBalance, 1);
        var typeProp = typeof(LeaveBalance).GetProperty("LeaveType");
        typeProp?.SetValue(leaveBalance, leaveType);

        var list = new List<LeaveBalance> { leaveBalance };
        _leaveRepository.GetBalancesByEmployeeAsync(1, 2026, Arg.Any<CancellationToken>()).Returns(list);

        var query = new GetLeaveBalancesQuery(1, 2026);
        var handler = new GetLeaveBalancesQueryHandler(_unitOfWork, _currentUser);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].LeaveTypeName.Should().Be("Annual Leave");
        result.Value[0].Available.Should().Be(10);
    }

    [Fact]
    public async Task GetLeaveTypes_ShouldReturnTypes_WhenAuthorized()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.LeaveView).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);

        var leaveType = LeaveType.Create(1, "Annual Leave", LeaveCategory.AnnualLeave, 10, true, true);
        SetId(leaveType, 1);
        var list = new List<LeaveType> { leaveType };
        _leaveRepository.GetTypesByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(list);

        var query = new GetLeaveTypesQuery(1);
        var handler = new GetLeaveTypesQueryHandler(_unitOfWork, _currentUser);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].TypeName.Should().Be("Annual Leave");
        result.Value[0].Category.Should().Be("AnnualLeave");
    }
}
