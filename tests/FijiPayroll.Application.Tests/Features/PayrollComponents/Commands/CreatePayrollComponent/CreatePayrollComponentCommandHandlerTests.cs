using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.PayrollComponents.Commands.CreatePayrollComponent;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FijiPayroll.Application.Tests.Features.PayrollComponents.Commands.CreatePayrollComponent;

/// <summary>
/// Unit tests for <see cref="CreatePayrollComponentCommandHandler"/>.
/// </summary>
public sealed class CreatePayrollComponentCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<CreatePayrollComponentCommandHandler> _logger;
    private readonly CreatePayrollComponentCommandHandler _handler;

    public CreatePayrollComponentCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _dateTime = Substitute.For<IDateTimeService>();
        _logger = Substitute.For<ILogger<CreatePayrollComponentCommandHandler>>();

        _handler = new CreatePayrollComponentCommandHandler(
            _unitOfWork,
            _currentUser,
            _dateTime,
            _logger);
    }

    [Fact]
    public async Task Handle_UserDoesNotHavePermission_ThrowsForbiddenAccessException()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsCreate).Returns(false);

        var command = new CreatePayrollComponentCommand(
            CompanyId: 1,
            ComponentCode: "BONUS",
            ComponentName: "Performance Bonus",
            ComponentType: ComponentType.Earning,
            CalculationMethod: CalculationMethod.Fixed,
            CalculationValue: 500m,
            Formula: null,
            IsTaxable: true,
            IsFnpfApplicable: true,
            DisplayOrder: 1,
            Description: "Bonus"
        );

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_UserDoesNotHaveCompanyAccess_ReturnsFailure()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsCreate).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(false);

        var command = new CreatePayrollComponentCommand(
            CompanyId: 1,
            ComponentCode: "BONUS",
            ComponentName: "Performance Bonus",
            ComponentType: ComponentType.Earning,
            CalculationMethod: CalculationMethod.Fixed,
            CalculationValue: 500m,
            Formula: null,
            IsTaxable: true,
            IsFnpfApplicable: true,
            DisplayOrder: 1,
            Description: "Bonus"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("You do not have access to company ID 1");
    }

    [Fact]
    public async Task Handle_CodeAlreadyExists_ReturnsFailure()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsCreate).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);
        _unitOfWork.PayrollComponents.CodeExistsAsync(1, "BONUS", null, Arg.Any<CancellationToken>()).Returns(true);

        var command = new CreatePayrollComponentCommand(
            CompanyId: 1,
            ComponentCode: "BONUS",
            ComponentName: "Performance Bonus",
            ComponentType: ComponentType.Earning,
            CalculationMethod: CalculationMethod.Fixed,
            CalculationValue: 500m,
            Formula: null,
            IsTaxable: true,
            IsFnpfApplicable: true,
            DisplayOrder: 1,
            Description: "Bonus"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists for this company");
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesPayrollComponentAndReturnsId()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsCreate).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);
        _currentUser.Username.Returns("test-user");
        _dateTime.UtcNow.Returns(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        _unitOfWork.PayrollComponents.CodeExistsAsync(1, "BONUS", null, Arg.Any<CancellationToken>()).Returns(false);

        var command = new CreatePayrollComponentCommand(
            CompanyId: 1,
            ComponentCode: "BONUS",
            ComponentName: "Performance Bonus",
            ComponentType: ComponentType.Earning,
            CalculationMethod: CalculationMethod.Fixed,
            CalculationValue: 500m,
            Formula: null,
            IsTaxable: true,
            IsFnpfApplicable: true,
            DisplayOrder: 1,
            Description: "Bonus"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.PayrollComponents.Received(1).AddAsync(Arg.Is<PayrollComponent>(x =>
            x.CompanyId == 1 &&
            x.ComponentCode == "BONUS" &&
            x.ComponentName == "Performance Bonus" &&
            x.ComponentType == ComponentType.Earning &&
            x.CalculationMethod == CalculationMethod.Fixed &&
            x.CalculationValue == 500m &&
            x.CreatedBy == "test-user"
        ), Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
