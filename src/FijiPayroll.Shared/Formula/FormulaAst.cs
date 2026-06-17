using System.Globalization;

namespace FijiPayroll.Shared.Formula;

public abstract class AstNode
{
    public abstract decimal Evaluate(Dictionary<string, decimal> variables);
    public abstract override string ToString();
}

public sealed class NumberNode : AstNode
{
    public NumberNode(decimal value)
    {
        Value = value;
    }

    public decimal Value { get; }

    public override decimal Evaluate(Dictionary<string, decimal> variables) => Value;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

public sealed class VariableNode : AstNode
{
    public VariableNode(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public override decimal Evaluate(Dictionary<string, decimal> variables)
    {
        if (variables.TryGetValue(Name, out decimal val))
        {
            return val;
        }
        if (variables.TryGetValue(Name.ToUpperInvariant(), out val))
        {
            return val;
        }
        throw new KeyNotFoundException($"Variable '{Name}' was not provided in the evaluation context.");
    }

    public override string ToString() => $"{{{Name}}}";
}

public sealed class BinaryOpNode : AstNode
{
    public BinaryOpNode(string op, AstNode left, AstNode right)
    {
        Op = op;
        Left = left;
        Right = right;
    }

    public string Op { get; }
    public AstNode Left { get; }
    public AstNode Right { get; }

    public override decimal Evaluate(Dictionary<string, decimal> variables)
    {
        var leftVal = Left.Evaluate(variables);
        var rightVal = Right.Evaluate(variables);

        return Op switch
        {
            "+" => leftVal + rightVal,
            "-" => leftVal - rightVal,
            "*" => leftVal * rightVal,
            "/" => rightVal == 0 ? throw new DivideByZeroException("Division by zero in formula evaluation.") : leftVal / rightVal,
            _ => throw new InvalidOperationException($"Unsupported binary operator '{Op}'.")
        };
    }

    public override string ToString() => $"({Left} {Op} {Right})";
}

public sealed class FunctionNode : AstNode
{
    public FunctionNode(string name, IReadOnlyList<AstNode> arguments)
    {
        Name = name.ToUpperInvariant();
        Arguments = arguments;
    }

    public string Name { get; }
    public IReadOnlyList<AstNode> Arguments { get; }

    public override decimal Evaluate(Dictionary<string, decimal> variables)
    {
        switch (Name)
        {
            case "ROUND":
                if (Arguments.Count != 2)
                    throw new ArgumentException("ROUND requires exactly 2 arguments.");
                var valToRound = Arguments[0].Evaluate(variables);
                var decimals = (int)Arguments[1].Evaluate(variables);
                return Math.Round(valToRound, decimals, MidpointRounding.AwayFromZero);

            case "IF":
                if (Arguments.Count != 3)
                    throw new ArgumentException("IF requires exactly 3 arguments.");
                var conditionVal = Arguments[0].Evaluate(variables);
                // Condition: non-zero is true, zero is false
                return conditionVal != 0
                    ? Arguments[1].Evaluate(variables)
                    : Arguments[2].Evaluate(variables);

            case "MIN":
                if (Arguments.Count == 0)
                    throw new ArgumentException("MIN requires at least 1 argument.");
                return Arguments.Select(a => a.Evaluate(variables)).Min();

            case "MAX":
                if (Arguments.Count == 0)
                    throw new ArgumentException("MAX requires at least 1 argument.");
                return Arguments.Select(a => a.Evaluate(variables)).Max();

            case "ABS":
                if (Arguments.Count != 1)
                    throw new ArgumentException("ABS requires exactly 1 argument.");
                return Math.Abs(Arguments[0].Evaluate(variables));

            default:
                throw new InvalidOperationException($"Unsupported function '{Name}'.");
        }
    }

    public override string ToString() => $"{Name}({string.Join(", ", Arguments.Select(a => a.ToString()))})";
}
