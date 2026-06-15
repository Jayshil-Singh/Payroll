using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.PayrollComponents.Queries.GetPayrollComponentById;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FijiPayroll.Application.Tests.Features.PayrollComponents.Queries.GetPayrollComponentById;

/// <summary>
/// Unit tests for <see cref="GetPayrollComponentByIdQueryHandler"/>.
/// </summary>
public sealed class GetPayrollComponentByIdQueryHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly GetPayrollComponentByIdQueryHandler _handler;

    public GetPayrollComponentByIdQueryHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _handler = new GetPayrollComponentByIdQueryHandler(_unitOfWork, _currentUser);
    }

    [Fact]
    public async Task Handle_UserDoesNotHavePermission_ThrowsForbiddenAccessException()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsView).Returns(false);

        var query = new GetPayrollComponentByIdQuery(1);

        // Act
        Func<Task> act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ComponentNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsView).Returns(true);
        _unitOfWork.PayrollComponents.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((PayrollComponent?)null);

        var query = new GetPayrollComponentByIdQuery(1);

        // Act
        Func<Task> act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_UserDoesNotHaveCompanyAccess_ThrowsForbiddenAccessException()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsView).Returns(true);
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

        var query = new GetPayrollComponentByIdQuery(1);

        // Act
        Func<Task> act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsDto()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsView).Returns(true);
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

        var query = new GetPayrollComponentByIdQuery(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.ComponentCode.Should().Be("TAX");
        result.Value.ComponentName.Should().Be("Tax component");
    }
}
