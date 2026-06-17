namespace FijiPayroll.Shared.Formula;

public sealed class CompiledRule
{
    public CompiledRule(AstNode rootNode, string expressionText, string compiledHash, IReadOnlyList<string> variablesUsed)
    {
        RootNode = rootNode;
        ExpressionText = expressionText;
        CompiledHash = compiledHash;
        VariablesUsed = variablesUsed;
    }

    public AstNode RootNode { get; }
    public string ExpressionText { get; }
    public string CompiledHash { get; }
    public IReadOnlyList<string> VariablesUsed { get; }

    public decimal Evaluate(Dictionary<string, decimal> variables)
    {
        return RootNode.Evaluate(variables);
    }
}
