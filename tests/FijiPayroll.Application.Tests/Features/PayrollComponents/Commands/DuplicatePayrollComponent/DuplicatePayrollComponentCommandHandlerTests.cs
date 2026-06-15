using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.PayrollComponents.Commands.DuplicatePayrollComponent;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FijiPayroll.Application.Tests.Features.PayrollComponents.Commands.DuplicatePayrollComponent;

/// <summary>
/// Unit tests for <see cref="DuplicatePayrollComponentCommandHandler"/>.
/// </summary>
public sealed class DuplicatePayrollComponentCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<DuplicatePayrollComponentCommandHandler> _logger;
    private readonly DuplicatePayrollComponentCommandHandler _handler;

    public DuplicatePayrollComponentCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _dateTime = Substitute.For<IDateTimeService>();
        _logger = Substitute.For<ILogger<DuplicatePayrollComponentCommandHandler>>();

        _handler = new DuplicatePayrollComponentCommandHandler(
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

        var command = new DuplicatePayrollComponentCommand(1, "NEWCODE", "New Component");

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_SourceComponentNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsCreate).Returns(true);
        _unitOfWork.PayrollComponents.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((PayrollComponent?)null);

        var command = new DuplicatePayrollComponentCommand(1, "NEWCODE", "New Component");

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_UserDoesNotHaveCompanyAccess_ReturnsFailure()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsCreate).Returns(true);
        var source = PayrollComponent.Create(
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
        _unitOfWork.PayrollComponents.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(source);
        _currentUser.HasCompanyAccess(1).Returns(false);

        var command = new DuplicatePayrollComponentCommand(1, "NEWCODE", "New Component");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("owning this component");
    }

    [Fact]
    public async Task Handle_NewCodeAlreadyExists_ReturnsFailure()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsCreate).Returns(true);
        var source = PayrollComponent.Create(
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
        _unitOfWork.PayrollComponents.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(source);
        _currentUser.HasCompanyAccess(1).Returns(true);
        _unitOfWork.PayrollComponents.CodeExistsAsync(1, "NEWCODE", null, Arg.Any<CancellationToken>()).Returns(true);

        var command = new DuplicatePayrollComponentCommand(1, "NEWCODE", "New Component");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Handle_ValidRequest_ClonesComponentAndReturnsNewId()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsCreate).Returns(true);
        var source = PayrollComponent.Create(
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
        _unitOfWork.PayrollComponents.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(source);
        _currentUser.HasCompanyAccess(1).Returns(true);
        _unitOfWork.PayrollComponents.CodeExistsAsync(1, "NEWCODE", null, Arg.Any<CancellationToken>()).Returns(false);
        _currentUser.Username.Returns("test-user");
        _dateTime.UtcNow.Returns(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        var command = new DuplicatePayrollComponentCommand(1, "NEWCODE", "New Component");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.PayrollComponents.Received(1).AddAsync(Arg.Is<PayrollComponent>(x =>
            x.CompanyId == 1 &&
            x.ComponentCode == "NEWCODE" &&
            x.ComponentName == "New Component" &&
            x.CalculationValue == 10m &&
            x.CreatedBy == "test-user"
        ), Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
