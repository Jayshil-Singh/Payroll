using FijiPayroll.Shared.Formula;
using FluentAssertions;
using System.Linq;
using Xunit;

namespace FijiPayroll.Domain.Tests.Formula;

/// <summary>
/// Unit tests validating circular dependency check and topological sorting in RuleDependencyGraph.
/// </summary>
public sealed class RuleDependencyGraphTests
{
    [Fact]
    public void Validate_ValidAcyclicGraph_ReturnsCorrectOrder()
    {
        // Arrange
        var graph = new RuleDependencyGraph();
        graph.AddNode("HOURLY_RATE");
        graph.AddDependency("BASIC", "HOURLY_RATE");
        graph.AddDependency("OVERTIME", "HOURLY_RATE");
        graph.AddDependency("GROSS", "BASIC");
        graph.AddDependency("GROSS", "OVERTIME");

        // Act
        var result = graph.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        
        // HOURLY_RATE must be calculated before BASIC and OVERTIME
        // BASIC and OVERTIME must be calculated before GROSS
        var order = result.CalculationOrder.ToList();
        order.IndexOf("HOURLY_RATE").Should().BeLessThan(order.IndexOf("BASIC"));
        order.IndexOf("HOURLY_RATE").Should().BeLessThan(order.IndexOf("OVERTIME"));
        order.IndexOf("BASIC").Should().BeLessThan(order.IndexOf("GROSS"));
        order.IndexOf("OVERTIME").Should().BeLessThan(order.IndexOf("GROSS"));
    }

    [Fact]
    public void Validate_GraphWithSimpleCycle_DetectsCircularDependency()
    {
        // Arrange
        var graph = new RuleDependencyGraph();
        graph.AddDependency("A", "B");
        graph.AddDependency("B", "A");

        // Act
        var result = graph.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().MatchRegex("Circular dependency detected: (A -> B -> A|B -> A -> B)");
    }

    [Fact]
    public void Validate_GraphWithLongerCycle_DetectsCircularDependency()
    {
        // Arrange
        var graph = new RuleDependencyGraph();
        graph.AddDependency("A", "B");
        graph.AddDependency("B", "C");
        graph.AddDependency("C", "A");

        // Act
        var result = graph.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().MatchRegex("Circular dependency detected: A -> B -> C -> A|B -> C -> A -> B|C -> A -> B -> C");
    }

    [Fact]
    public void Validate_SelfReferencingNode_DetectsCircularDependency()
    {
        // Arrange
        var graph = new RuleDependencyGraph();
        graph.AddDependency("A", "A");

        // Act
        var result = graph.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Match("Circular dependency detected: A -> A");
    }
}
