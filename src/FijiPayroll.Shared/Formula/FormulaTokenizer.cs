using System.Text;

namespace FijiPayroll.Shared.Formula;

/// <summary>
/// Tokenizes raw formula expressions into lexical tokens.
/// </summary>
public sealed class FormulaTokenizer
{
    private static readonly HashSet<string> Functions = new(StringComparer.OrdinalIgnoreCase)
    {
        "ROUND", "IF", "MIN", "MAX", "ABS"
    };

    public IReadOnlyList<FormulaToken> Tokenize(string expression)
    {
        var tokens = new List<FormulaToken>();
        if (string.IsNullOrWhiteSpace(expression))
        {
            return tokens;
        }

        int index = 0;
        while (index < expression.Length)
        {
            char ch = expression[index];

            if (char.IsWhiteSpace(ch))
            {
                index++;
                continue;
            }

            if (ch == '{')
            {
                int start = index;
                index++; // skip '{'
                var sb = new StringBuilder();
                while (index < expression.Length && expression[index] != '}')
                {
                    sb.Append(expression[index]);
                    index++;
                }

                if (index >= expression.Length)
                {
                    throw new FormatException($"Unclosed variable bracket starting at position {start}.");
                }

                index++; // skip '}'
                tokens.Add(new FormulaToken(TokenType.Identifier, sb.ToString(), start));
                continue;
            }

            if (char.IsDigit(ch) || ch == '.')
            {
                int start = index;
                var sb = new StringBuilder();
                bool hasDot = false;
                while (index < expression.Length && (char.IsDigit(expression[index]) || expression[index] == '.'))
                {
                    if (expression[index] == '.')
                    {
                        if (hasDot) throw new FormatException($"Invalid number formatting at position {index}.");
                        hasDot = true;
                    }
                    sb.Append(expression[index]);
                    index++;
                }
                tokens.Add(new FormulaToken(TokenType.Number, sb.ToString(), start));
                continue;
            }

            if (char.IsLetter(ch))
            {
                int start = index;
                var sb = new StringBuilder();
                while (index < expression.Length && (char.IsLetterOrDigit(expression[index]) || expression[index] == '_'))
                {
                    sb.Append(expression[index]);
                    index++;
                }
                string word = sb.ToString();

                int temp = index;
                while (temp < expression.Length && char.IsWhiteSpace(expression[temp]))
                {
                    temp++;
                }
                bool isFunctionCall = temp < expression.Length && expression[temp] == '(';

                if (isFunctionCall)
                {
                    if (Functions.Contains(word))
                    {
                        tokens.Add(new FormulaToken(TokenType.FunctionName, word.ToUpperInvariant(), start));
                    }
                    else
                    {
                        throw new FormatException($"Function '{word}' is not supported in this version.");
                    }
                }
                else
                {
                    tokens.Add(new FormulaToken(TokenType.Identifier, word, start));
                }
                continue;
            }

            if (ch is '+' or '-' or '*' or '/')
            {
                tokens.Add(new FormulaToken(TokenType.Operator, ch.ToString(), index));
                index++;
                continue;
            }

            if (ch == '(')
            {
                tokens.Add(new FormulaToken(TokenType.LeftParenthesis, "(", index));
                index++;
                continue;
            }

            if (ch == ')')
            {
                tokens.Add(new FormulaToken(TokenType.RightParenthesis, ")", index));
                index++;
                continue;
            }

            if (ch == ',')
            {
                tokens.Add(new FormulaToken(TokenType.Comma, ",", index));
                index++;
                continue;
            }

            throw new FormatException($"Invalid character '{ch}' at position {index}.");
        }

        tokens.Add(new FormulaToken(TokenType.EndOfFile, string.Empty, index));
        return tokens;
    }
}
