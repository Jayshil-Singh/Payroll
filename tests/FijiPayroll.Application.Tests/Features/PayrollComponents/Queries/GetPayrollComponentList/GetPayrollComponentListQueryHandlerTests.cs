using FijiPayroll.Application.Common.Exceptions;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.PayrollComponents.Queries.GetPayrollComponentList;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FijiPayroll.Application.Tests.Features.PayrollComponents.Queries.GetPayrollComponentList;

/// <summary>
/// Unit tests for <see cref="GetPayrollComponentListQueryHandler"/>.
/// </summary>
public sealed class GetPayrollComponentListQueryHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly GetPayrollComponentListQueryHandler _handler;

    public GetPayrollComponentListQueryHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _handler = new GetPayrollComponentListQueryHandler(_unitOfWork, _currentUser);
    }

    [Fact]
    public async Task Handle_UserDoesNotHavePermission_ThrowsForbiddenAccessException()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsView).Returns(false);

        var query = new GetPayrollComponentListQuery(1);

        // Act
        Func<Task> act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_UserDoesNotHaveCompanyAccess_ThrowsForbiddenAccessException()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsView).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(false);

        var query = new GetPayrollComponentListQuery(1);

        // Act
        Func<Task> act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ValidRequest_AppliesFiltersAndReturnsPaginatedList()
    {
        // Arrange
        _currentUser.HasPermission(PermissionConstants.PayrollComponentsView).Returns(true);
        _currentUser.HasCompanyAccess(1).Returns(true);

        var component1 = PayrollComponent.Create(
            companyId: 1,
            componentCode: "BASIC",
            componentName: "Basic Salary",
            componentType: ComponentType.Earning,
            calculationMethod: CalculationMethod.Manual,
            calculationValue: null,
            formula: null,
            isTaxable: true,
            isFnpfApplicable: true,
            displayOrder: 1
        );

        var component2 = PayrollComponent.Create(
            companyId: 1,
            componentCode: "HRA",
            componentName: "House Rent Allowance",
            componentType: ComponentType.Allowance,
            calculationMethod: CalculationMethod.Fixed,
            calculationValue: 500m,
            formula: null,
            isTaxable: true,
            isFnpfApplicable: true,
            displayOrder: 2
        );

        var component3 = PayrollComponent.Create(
            companyId: 1,
            componentCode: "BONUS",
            componentName: "Bonus Pay",
            componentType: ComponentType.Earning,
            calculationMethod: CalculationMethod.Fixed,
            calculationValue: 200m,
            formula: null,
            isTaxable: true,
            isFnpfApplicable: true,
            displayOrder: 3
        );
        component3.Deactivate();

        var list = new List<PayrollComponent> { component1 };
        _unitOfWork.PayrollComponents.GetPagedAsync(
            1, "Salary", null, true, 1, 10, Arg.Any<CancellationToken>())
            .Returns((list.AsReadOnly(), 1));

        var query = new GetPayrollComponentListQuery(
            CompanyId: 1,
            SearchTerm: "Salary",
            ComponentTypeFilter: null,
            ActiveOnly: true,
            PageNumber: 1,
            PageSize: 10
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].ComponentCode.Should().Be("BASIC");
    }
}
