using FijiPayroll.Shared.Formula;
using FluentAssertions;
using Xunit;

namespace FijiPayroll.Domain.Tests.Formula;

public sealed class FormulaParserTests
{
    [Theory]
    [InlineData("10 + 20", 30)]
    [InlineData("10 - 5", 5)]
    [InlineData("10 * 2.5", 25)]
    [InlineData("10 / 4", 2.5)]
    [InlineData("(10 + 5) * 2", 30)]
    [InlineData("10 + 5 * 2", 20)]
    public void Evaluate_SimpleArithmetic_ReturnsExpectedResult(string expression, decimal expected)
    {
        var generator = new AstGenerator();
        var compiled = generator.Compile(expression);
        var result = compiled.Evaluate(new Dictionary<string, decimal>());
        result.Should().Be(expected);
    }

    [Fact]
    public void Evaluate_WithVariables_ResolvesAndCalculates()
    {
        var generator = new AstGenerator();
        var compiled = generator.Compile("{BASIC} * 0.10 + {Bonus}");
        var variables = new Dictionary<string, decimal>
        {
            { "BASIC", 2000m },
            { "Bonus", 150m }
        };

        var result = compiled.Evaluate(variables);
        result.Should().Be(350m);
    }

    [Theory]
    [InlineData("ROUND(10.556, 2)", 10.56)]
    [InlineData("ABS(-20.5)", 20.5)]
    [InlineData("MIN(10, 5, 20)", 5)]
    [InlineData("MAX(10, 5, 20)", 20)]
    [InlineData("IF(1, 100, 200)", 100)]
    [InlineData("IF(0, 100, 200)", 200)]
    public void Evaluate_BuiltInFunctions_ComputesCorrectly(string expression, decimal expected)
    {
        var generator = new AstGenerator();
        var compiled = generator.Compile(expression);
        var result = compiled.Evaluate(new Dictionary<string, decimal>());
        result.Should().Be(expected);
    }

    [Fact]
    public void Compile_ConstantExpression_FoldsConstants()
    {
        var generator = new AstGenerator();
        var compiled = generator.Compile("ROUND(10 * 5 + 100, 1)");

        compiled.RootNode.Should().BeOfType<NumberNode>();
        var numNode = (NumberNode)compiled.RootNode;
        numNode.Value.Should().Be(150m);
    }

    [Theory]
    [InlineData("SUM(1, 2)")]
    [InlineData("LOOKUP(a, b)")]
    [InlineData("DATEDIFF(x, y)")]
    [InlineData("AGE(z)")]
    public void Compile_ForbiddenFunctions_ThrowsException(string expression)
    {
        var generator = new AstGenerator();
        Action act = () => generator.Compile(expression);
        act.Should().Throw<FormatException>().WithMessage("*not supported in this version*");
    }
}
