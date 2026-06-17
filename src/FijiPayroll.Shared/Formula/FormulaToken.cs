namespace FijiPayroll.Shared.Formula;

public enum TokenType
{
    Number,
    Identifier,
    Operator,
    LeftParenthesis,
    RightParenthesis,
    Comma,
    FunctionName,
    EndOfFile
}

public sealed class FormulaToken
{
    public FormulaToken(TokenType type, string value, int position)
    {
        Type = type;
        Value = value;
        Position = position;
    }

    public TokenType Type { get; }
    public string Value { get; }
    public int Position { get; }

    public override string ToString() => $"{Type}: '{Value}' at {Position}";
}
