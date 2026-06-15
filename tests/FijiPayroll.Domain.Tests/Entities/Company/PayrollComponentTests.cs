using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace FijiPayroll.Domain.Tests.Entities.Company;

/// <summary>
/// Unit tests for <see cref="PayrollComponent"/> domain entity.
/// </summary>
public sealed class PayrollComponentTests
{
    [Fact]
    public void Create_ValidFixedComponent_CreatesInstance()
    {
        // Act
        var component = PayrollComponent.Create(
            companyId: 1,
            componentCode: "BASIC",
            componentName: "Basic Pay",
            componentType: ComponentType.Earning,
            calculationMethod: CalculationMethod.Fixed,
            calculationValue: 1500m,
            formula: null,
            isTaxable: true,
            isFnpfApplicable: true,
            displayOrder: 1,
            description: "Basic salary component"
        );

        // Assert
        component.Should().NotBeNull();
        component.CompanyId.Should().Be(1);
        component.ComponentCode.Should().Be("BASIC");
        component.ComponentName.Should().Be("Basic Pay");
        component.ComponentType.Should().Be(ComponentType.Earning);
        component.CalculationMethod.Should().Be(CalculationMethod.Fixed);
        component.CalculationValue.Should().Be(1500m);
        component.Formula.Should().BeNull();
        component.IsTaxable.Should().BeTrue();
        component.IsFnpfApplicable.Should().BeTrue();
        component.DisplayOrder.Should().Be(1);
        component.Description.Should().Be("Basic salary component");
        component.IsActive.Should().BeTrue();
        component.IsSystemComponent.Should().BeFalse();
    }

    [Fact]
    public void Create_FixedComponentMissingValue_ThrowsDomainException()
    {
        // Act
        Action act = () => PayrollComponent.Create(
            companyId: 1,
            componentCode: "BASIC",
            componentName: "Basic Pay",
            componentType: ComponentType.Earning,
            calculationMethod: CalculationMethod.Fixed,
            calculationValue: null, // Invalid for Fixed
            formula: null,
            isTaxable: true,
            isFnpfApplicable: true,
            displayOrder: 1
        );

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*CalculationValue must be a positive number*");
    }

    [Fact]
    public void Create_FormulaComponentMissingFormula_ThrowsDomainException()
    {
        // Act
        Action act = () => PayrollComponent.Create(
            companyId: 1,
            componentCode: "OT",
            componentName: "Overtime Pay",
            componentType: ComponentType.Earning,
            calculationMethod: CalculationMethod.Formula,
            calculationValue: null,
            formula: " ", // Invalid formula
            isTaxable: true,
            isFnpfApplicable: true,
            displayOrder: 1
        );

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Formula expression must not be empty*");
    }

    [Fact]
    public void Deactivate_SystemComponent_ThrowsDomainException()
    {
        // Arrange
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
            description: null,
            isSystemComponent: true // System component
        );

        // Act
        Action act = () => component.Deactivate();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*cannot be deactivated*");
    }

    [Fact]
    public void SoftDelete_SystemComponent_ThrowsDomainException()
    {
        // Arrange
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
            description: null,
            isSystemComponent: true // System component
        );

        // Act
        Action act = () => component.SoftDelete("admin");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*cannot be deleted*");
    }

    [Fact]
    public void Duplicate_ValidComponent_ReturnsCloneWithNewCodeAndName()
    {
        // Arrange
        var source = PayrollComponent.Create(
            companyId: 1,
            componentCode: "BASIC",
            componentName: "Basic Pay",
            componentType: ComponentType.Earning,
            calculationMethod: CalculationMethod.Fixed,
            calculationValue: 1500m,
            formula: null,
            isTaxable: true,
            isFnpfApplicable: true,
            displayOrder: 1
        );

        // Act
        var duplicate = source.Duplicate("BASIC_COPY", "Copy of Basic Pay");

        // Assert
        duplicate.Should().NotBeNull();
        duplicate.CompanyId.Should().Be(1);
        duplicate.ComponentCode.Should().Be("BASIC_COPY");
        duplicate.ComponentName.Should().Be("Copy of Basic Pay");
        duplicate.CalculationValue.Should().Be(1500m);
        duplicate.IsSystemComponent.Should().BeFalse();
        duplicate.IsActive.Should().BeTrue();
    }
}
