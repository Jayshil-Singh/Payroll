using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.PayrollComponents.Commands.UpdatePayrollComponent;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FijiPayroll.Application.Tests.Features.PayrollComponents.Commands.UpdatePayrollComponent;

/// <summary>
/// Unit tests for <see cref="UpdatePayrollComponentCommandHandler"/>.
/// </summary>
public sealed class UpdatePayrollComponentCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<UpdatePayrollComponentCommandHandler> _logger;
    private readonly UpdatePayrollComponentCommandHandler _handler;

    public UpdatePayrollComponentCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _dateTime = Substitute.For<IDateTimeService>();
        _logger = Substitute.For<ILogger<UpdatePayrollComponentCommandHandler>>();

        _handler = new UpdatePayrollComponentCommandHandler(
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

        var command = new UpdatePayrollComponentCommand(
            Id: 1,
            ComponentName: "Updated Name",
            ComponentType: ComponentType.Earning,
            CalculationMethod: CalculationMethod.Fixed,
            CalculationValue: 100m,
            Formula: null,
            IsTaxable: true,
            IsFnpfApplicable: true,
            DisplayOrder: 1,
            Description: "Desc"
        );

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

        var command = new UpdatePayrollComponentCommand(
            Id: 1,
            ComponentName: "Updated Name",
            ComponentType: ComponentType.Earning,
            CalculationMethod: CalculationMethod.Fixed,
            CalculationValue: 100m,
            Formula: null,
            IsTaxable: true,
            IsFnpfApplicable: true,
            DisplayOrder: 1,
            Description: "Desc"
        );

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

        var command = new UpdatePayrollComponentCommand(
            Id: 1,
            ComponentName: "Updated Name",
            ComponentType: ComponentType.Earning,
            CalculationMethod: CalculationMethod.Fixed,
            CalculationValue: 100m,
            Formula: null,
            IsTaxable: true,
            IsFnpfApplicable: true,
            DisplayOrder: 1,
            Description: "Desc"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("owning this component");
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesComponent()
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
        _currentUser.HasCompanyAccess(1).Returns(true);
        _currentUser.Username.Returns("test-user");
        _dateTime.UtcNow.Returns(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        var command = new UpdatePayrollComponentCommand(
            Id: 1,
            ComponentName: "Updated Tax Component",
            ComponentType: ComponentType.Statutory,
            CalculationMethod: CalculationMethod.Percentage,
            CalculationValue: 8m,
            Formula: null,
            IsTaxable: false,
            IsFnpfApplicable: false,
            DisplayOrder: 2,
            Description: "Updated Tax"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        component.ComponentName.Should().Be("Updated Tax Component");
        component.CalculationMethod.Should().Be(CalculationMethod.Percentage);
        component.CalculationValue.Should().Be(8m);
        component.DisplayOrder.Should().Be(2);
        component.ModifiedBy.Should().Be("test-user");

        _unitOfWork.PayrollComponents.Received(1).Update(component);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
