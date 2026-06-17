namespace FijiPayroll.Shared.Formula;

/// <summary>
/// Optimizes the formula AST by folding constant expressions.
/// </summary>
public sealed class FormulaOptimizer
{
    public AstNode Optimize(AstNode node)
    {
        if (node is NumberNode || node is VariableNode)
        {
            return node;
        }

        if (node is BinaryOpNode binaryNode)
        {
            var optimizedLeft = Optimize(binaryNode.Left);
            var optimizedRight = Optimize(binaryNode.Right);

            if (optimizedLeft is NumberNode leftNum && optimizedRight is NumberNode rightNum)
            {
                // Fold constant binary operation
                decimal result = binaryNode.Op switch
                {
                    "+" => leftNum.Value + rightNum.Value,
                    "-" => leftNum.Value - rightNum.Value,
                    "*" => leftNum.Value * rightNum.Value,
                    "/" => rightNum.Value == 0 ? throw new DivideByZeroException("Division by zero in constant folding.") : leftNum.Value / rightNum.Value,
                    _ => throw new InvalidOperationException($"Unsupported binary operator '{binaryNode.Op}'.")
                };
                return new NumberNode(result);
            }

            return new BinaryOpNode(binaryNode.Op, optimizedLeft, optimizedRight);
        }

        if (node is FunctionNode funcNode)
        {
            var optimizedArgs = funcNode.Arguments.Select(Optimize).ToList();

            if (optimizedArgs.All(a => a is NumberNode))
            {
                // Fold constant function call
                var dummyVariables = new Dictionary<string, decimal>();
                var evalNode = new FunctionNode(funcNode.Name, optimizedArgs);
                decimal result = evalNode.Evaluate(dummyVariables);
                return new NumberNode(result);
            }

            return new FunctionNode(funcNode.Name, optimizedArgs);
        }

        return node;
    }
}
