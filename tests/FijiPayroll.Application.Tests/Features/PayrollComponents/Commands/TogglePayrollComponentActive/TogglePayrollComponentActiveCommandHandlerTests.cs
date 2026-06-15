using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.PayrollComponents.Commands.TogglePayrollComponentActive;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FijiPayroll.Application.Tests.Features.PayrollComponents.Commands.TogglePayrollComponentActive;

/// <summary>
/// Unit tests for <see cref="TogglePayrollComponentActiveCommandHandler"/>.
/// </summary>
public sealed class TogglePayrollComponentActiveCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<TogglePayrollComponentActiveCommandHandler> _logger;
    private readonly TogglePayrollComponentActiveCommandHandler _handler;

    public TogglePayrollComponentActiveCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _dateTime = Substitute.For<IDateTimeService>();
        _logger = Substitute.For<ILogger<TogglePayrollComponentActiveCommandHandler>>();

        _handler = new TogglePayrollComponentActiveCommandHandler(
            _unitOfWork,
            _currentUser,
            _dateTime,
            _logger);
    }

    [Fact]
    public async Task Handle_UserDoesNotHavePermission_ThrowsForbiddenAccessException()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsEdit).Returns(false);

        var command = new TogglePayrollComponentActiveCommand(1, true);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ComponentNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsEdit).Returns(true);
        _unitOfWork.PayrollComponents.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((PayrollComponent?)null);

        var command = new TogglePayrollComponentActiveCommand(1, true);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_UserDoesNotHaveCompanyAccess_ReturnsFailure()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsEdit).Returns(true);
        var component = PayrollComponent.Create(
            companyId: 1,
            componentCode: "TAX",
            componentName: "Tax component",
            componentType: ComponentType.Statutory,
            calculationMethod: CalculationMethod.Fixed,
            calculationValue: 10m,
            formula: null,
            isTaxable: true,
            isFnpfApplicable: true,
            displayOrder: 1,
            description: "Tax"
        );
        _unitOfWork.PayrollComponents.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(component);
        _currentUser.HasCompanyAccess(1).Returns(false);

        var command = new TogglePayrollComponentActiveCommand(1, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("owning this component");
    }

    [Fact]
    public async Task Handle_DeactivateSystemComponent_ReturnsFailure()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsEdit).Returns(true);
        var component = PayrollComponent.Create(
            companyId: 1,
            componentCode: "PAYE",
            componentName: "PAYE Tax",
            componentType: ComponentType.Statutory,
            calculationMethod: CalculationMethod.Manual,
            calculationValue: null,
            formula: null,
            isTaxable: true,
            isFnpfApplicable: true,
            displayOrder: 1,
            description: "PAYE",
            isSystemComponent: true
        );
        _unitOfWork.PayrollComponents.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(component);
        _currentUser.HasCompanyAccess(1).Returns(true);

        var command = new TogglePayrollComponentActiveCommand(1, false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("cannot be deactivated");
    }

    [Fact]
    public async Task Handle_ValidDeactivate_DeactivatesComponent()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsEdit).Returns(true);
        var component = PayrollComponent.Create(
            companyId: 1,
            componentCode: "BONUS",
            componentName: "Bonus",
            componentType: ComponentType.Earning,
            calculationMethod: CalculationMethod.Fixed,
            calculationValue: 10m,
            formula: null,
            isTaxable: true,
            isFnpfApplicable: true,
            displayOrder: 1,
            description: "Bonus"
        );
        _unitOfWork.PayrollComponents.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(component);
        _currentUser.HasCompanyAccess(1).Returns(true);
        _currentUser.Username.Returns("test-user");
        _dateTime.UtcNow.Returns(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        var command = new TogglePayrollComponentActiveCommand(1, false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        component.IsActive.Should().BeFalse();
        component.ModifiedBy.Should().Be("test-user");

        _unitOfWork.PayrollComponents.Received(1).Update(component);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidActivate_ActivatesComponent()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsEdit).Returns(true);
        var component = PayrollComponent.Create(
            companyId: 1,
            componentCode: "BONUS",
            componentName: "Bonus",
            componentType: ComponentType.Earning,
            calculationMethod: CalculationMethod.Fixed,
            calculationValue: 10m,
            formula: null,
            isTaxable: true,
            isFnpfApplicable: true,
            displayOrder: 1,
            description: "Bonus"
        );
        component.Deactivate(); // Start as inactive

        _unitOfWork.PayrollComponents.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(component);
        _currentUser.HasCompanyAccess(1).Returns(true);
        _currentUser.Username.Returns("test-user");
        _dateTime.UtcNow.Returns(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        var command = new TogglePayrollComponentActiveCommand(1, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        component.IsActive.Should().BeTrue();
        component.ModifiedBy.Should().Be("test-user");

        _unitOfWork.PayrollComponents.Received(1).Update(component);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
