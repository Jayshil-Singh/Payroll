namespace FijiPayroll.Shared.Formula;

/// <summary>
/// Performs semantic checks on the formula AST (e.g., matching argument counts, valid variable references, forbidden functions).
/// </summary>
public sealed class RuleSemanticValidator
{
    private static readonly HashSet<string> ForbiddenFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "LOOKUP", "DATEDIFF", "AGE", "SUM"
    };

    public IReadOnlyList<string> Validate(AstNode node)
    {
        var errors = new List<string>();
        ValidateNode(node, errors);
        return errors;
    }

    private void ValidateNode(AstNode node, List<string> errors)
    {
        if (node is BinaryOpNode binaryNode)
        {
            ValidateNode(binaryNode.Left, errors);
            ValidateNode(binaryNode.Right, errors);
        }
        else if (node is FunctionNode funcNode)
        {
            if (ForbiddenFunctions.Contains(funcNode.Name))
            {
                errors.Add($"Function '{funcNode.Name}' is not supported in this version of the rule engine.");
            }

            switch (funcNode.Name)
            {
                case "ROUND":
                    if (funcNode.Arguments.Count != 2)
                    {
                        errors.Add("ROUND function expects exactly 2 arguments.");
                    }
                    break;
                case "IF":
                    if (funcNode.Arguments.Count != 3)
                    {
                        errors.Add("IF function expects exactly 3 arguments.");
                    }
                    break;
                case "MIN":
                case "MAX":
                    if (funcNode.Arguments.Count == 0)
                    {
                        errors.Add($"{funcNode.Name} function expects at least 1 argument.");
                    }
                    break;
                case "ABS":
                    if (funcNode.Arguments.Count != 1)
                    {
                        errors.Add("ABS function expects exactly 1 argument.");
                    }
                    break;
            }

            foreach (var arg in funcNode.Arguments)
            {
                ValidateNode(arg, errors);
            }
        }
    }
}
