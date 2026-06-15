using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.PayrollComponents.Commands.DeletePayrollComponent;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FijiPayroll.Application.Tests.Features.PayrollComponents.Commands.DeletePayrollComponent;

/// <summary>
/// Unit tests for <see cref="DeletePayrollComponentCommandHandler"/>.
/// </summary>
public sealed class DeletePayrollComponentCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<DeletePayrollComponentCommandHandler> _logger;
    private readonly DeletePayrollComponentCommandHandler _handler;

    public DeletePayrollComponentCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _logger = Substitute.For<ILogger<DeletePayrollComponentCommandHandler>>();

        _handler = new DeletePayrollComponentCommandHandler(
            _unitOfWork,
            _currentUser,
            _logger);
    }

    [Fact]
    public async Task Handle_UserDoesNotHavePermission_ThrowsForbiddenAccessException()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsDelete).Returns(false);

        var command = new DeletePayrollComponentCommand(1);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ComponentNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsDelete).Returns(true);
        _unitOfWork.PayrollComponents.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((PayrollComponent?)null);

        var command = new DeletePayrollComponentCommand(1);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_UserDoesNotHaveCompanyAccess_ReturnsFailure()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsDelete).Returns(true);
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

        var command = new DeletePayrollComponentCommand(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("access to the company");
    }

    [Fact]
    public async Task Handle_SystemComponent_ReturnsFailure()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsDelete).Returns(true);
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

        var command = new DeletePayrollComponentCommand(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("system component and cannot be deleted");
    }

    [Fact]
    public async Task Handle_ComponentUsedInPayrollRuns_ReturnsFailure()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsDelete).Returns(true);
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
        _unitOfWork.PayrollComponents.IsUsedInPayrollRunsAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        var command = new DeletePayrollComponentCommand(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("cannot be deleted because it has been used");
    }

    [Fact]
    public async Task Handle_ValidRequest_SoftDeletesComponent()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsDelete).Returns(true);
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
        _unitOfWork.PayrollComponents.IsUsedInPayrollRunsAsync(1, Arg.Any<CancellationToken>()).Returns(false);

        var command = new DeletePayrollComponentCommand(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        component.IsDeleted.Should().BeTrue();
        component.DeletedBy.Should().Be("test-user");
        component.IsActive.Should().BeFalse();

        _unitOfWork.PayrollComponents.Received(1).Update(component);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
