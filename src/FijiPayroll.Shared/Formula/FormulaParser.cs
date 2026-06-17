namespace FijiPayroll.Shared.Formula;

/// <summary>
/// Parses lexical tokens into an executable AST.
/// </summary>
public sealed class FormulaParser
{
    private IReadOnlyList<FormulaToken> _tokens = Array.Empty<FormulaToken>();
    private int _index;

    public AstNode Parse(IReadOnlyList<FormulaToken> tokens)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _index = 0;

        var node = ParseExpression();

        if (Current.Type != TokenType.EndOfFile)
        {
            throw new FormatException($"Unexpected token '{Current.Value}' at position {Current.Position}. Expected end of file.");
        }

        return node;
    }

    private FormulaToken Current => _index < _tokens.Count ? _tokens[_index] : new FormulaToken(TokenType.EndOfFile, string.Empty, _tokens.Count);

    private FormulaToken Consume(TokenType expectedType)
    {
        var token = Current;
        if (token.Type != expectedType)
        {
            throw new FormatException($"Expected token type {expectedType} but found '{token.Value}' at position {token.Position}.");
        }
        _index++;
        return token;
    }

    private AstNode ParseExpression()
    {
        return ParseAdditive();
    }

    private AstNode ParseAdditive()
    {
        var left = ParseMultiplicative();

        while (Current.Type == TokenType.Operator && (Current.Value == "+" || Current.Value == "-"))
        {
            var opToken = Current;
            _index++; // consume operator
            var right = ParseMultiplicative();
            left = new BinaryOpNode(opToken.Value, left, right);
        }

        return left;
    }

    private AstNode ParseMultiplicative()
    {
        var left = ParseUnary();

        while (Current.Type == TokenType.Operator && (Current.Value == "*" || Current.Value == "/"))
        {
            var opToken = Current;
            _index++; // consume operator
            var right = ParseUnary();
            left = new BinaryOpNode(opToken.Value, left, right);
        }

        return left;
    }

    private AstNode ParseUnary()
    {
        if (Current.Type == TokenType.Operator && (Current.Value == "+" || Current.Value == "-"))
        {
            var opToken = Current;
            _index++; // consume operator
            var operand = ParseUnary();
            if (opToken.Value == "-")
            {
                return new BinaryOpNode("-", new NumberNode(0), operand);
            }
            return operand;
        }
        return ParsePrimary();
    }

    private AstNode ParsePrimary()
    {
        var token = Current;

        if (token.Type == TokenType.Number)
        {
            _index++;
            if (decimal.TryParse(token.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal val))
            {
                return new NumberNode(val);
            }
            throw new FormatException($"Invalid number literal '{token.Value}' at position {token.Position}.");
        }

        if (token.Type == TokenType.Identifier)
        {
            _index++;
            return new VariableNode(token.Value);
        }

        if (token.Type == TokenType.FunctionName)
        {
            _index++;
            Consume(TokenType.LeftParenthesis);
            var arguments = new List<AstNode>();

            if (Current.Type != TokenType.RightParenthesis)
            {
                arguments.Add(ParseExpression());
                while (Current.Type == TokenType.Comma)
                {
                    Consume(TokenType.Comma);
                    arguments.Add(ParseExpression());
                }
            }

            Consume(TokenType.RightParenthesis);
            return new FunctionNode(token.Value, arguments);
        }

        if (token.Type == TokenType.LeftParenthesis)
        {
            _index++;
            var node = ParseExpression();
            Consume(TokenType.RightParenthesis);
            return node;
        }

        throw new FormatException($"Unexpected token '{token.Value}' at position {token.Position}.");
    }
}
