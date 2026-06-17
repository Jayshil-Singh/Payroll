using System.Security.Cryptography;
using System.Text;

namespace FijiPayroll.Shared.Formula;

public sealed class AstGenerator
{
    private readonly FormulaTokenizer _tokenizer = new();
    private readonly FormulaParser _parser = new();
    private readonly RuleSemanticValidator _validator = new();
    private readonly FormulaOptimizer _optimizer = new();

    public CompiledRule Compile(string expressionText)
    {
        if (expressionText == null) throw new ArgumentNullException(nameof(expressionText));

        var tokens = _tokenizer.Tokenize(expressionText);
        var ast = _parser.Parse(tokens);

        var errors = _validator.Validate(ast);
        if (errors.Count > 0)
        {
            throw new FormatException($"Semantic validation failed: {string.Join("; ", errors)}");
        }

        var optimizedAst = _optimizer.Optimize(ast);
        var hash = CalculateSha256(expressionText);
        var variables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectVariables(optimizedAst, variables);

        return new CompiledRule(optimizedAst, expressionText, hash, variables.ToList());
    }

    private static string CalculateSha256(string text)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        var sb = new StringBuilder();
        foreach (byte b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    private static void CollectVariables(AstNode node, HashSet<string> variables)
    {
        if (node is VariableNode varNode)
        {
            variables.Add(varNode.Name);
        }
        else if (node is BinaryOpNode binNode)
        {
            CollectVariables(binNode.Left, variables);
            CollectVariables(binNode.Right, variables);
        }
        else if (node is FunctionNode funcNode)
        {
            foreach (var arg in funcNode.Arguments)
            {
                CollectVariables(arg, variables);
            }
        }
    }
}
