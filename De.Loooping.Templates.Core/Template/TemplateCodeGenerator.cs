using System.Text;
using De.Loooping.Templates.Core.TemplateProcessors;
using De.Loooping.Templates.Core.Tokenizers;
using Microsoft.CodeAnalysis.CSharp;

namespace De.Loooping.Templates.Core.Template;

internal class TemplateCodeGenerator
{
    public string Generate(IEnumerable<Token> tokens)
    {
        StringBuilder sb = new();
        IEnumerator<Token> enumerator = tokens.GetEnumerator();
        EvaluateRoot(enumerator, sb);
        return sb.ToString();
    }
    
    private void EvaluateRoot(IEnumerator<Token> tokenEnumerator, StringBuilder codeBuilder)
    {
        while (tokenEnumerator.MoveNext())
        {
            var token = tokenEnumerator.Current;
            switch (token.TokenType)
            {
                case TokenType.Literal:
                    string literal = SymbolDisplay.FormatLiteral(token.Value, true);
                    codeBuilder.AppendLine($"yield return {literal};");
                    break;
                case TokenType.LeftCommentDelimiter:
                    EvaluateComment(tokenEnumerator, codeBuilder);
                    break;
                case TokenType.LeftContentDelimiter:
                    EvaluateContent(tokenEnumerator, codeBuilder);
                    break;
                case TokenType.LeftStatementDelimiter:
                    EvaluateStatement(tokenEnumerator, codeBuilder);
                    break;
                default:
                    throw new SyntaxException(); // TODO: add specific information about position and kind of the error
            }
        }
    }

    private void EvaluateStatement(IEnumerator<Token> tokenEnumerator, StringBuilder codeBuilder)
    {
        while (tokenEnumerator.MoveNext())
        {
            var token = tokenEnumerator.Current;
            switch (token.TokenType)
            {
                case TokenType.CSharp:
                    codeBuilder.AppendLine(token.Value);
                    break;
                case TokenType.RightStatementDelimiter:
                    return;
                default:
                    throw new SyntaxException(); // TODO: add specific information about position and kind of the error
            }
        }
    }

    private void EvaluateContent(IEnumerator<Token> tokenEnumerator, StringBuilder codeBuilder)
    {
        while (tokenEnumerator.MoveNext())
        {
            var token = tokenEnumerator.Current;
            switch (token.TokenType)
            {
                case TokenType.CSharp:
                    string code = token.Value.Trim();
                    if (code.Length == 0)
                    {
                        // TODO: add specific information about the position of the error
                        throw new SyntaxException("Content expression must not be empty");
                    }

                    if (code.Last() != ';')
                    {
                        code += ";";
                    }

                    codeBuilder.AppendLine($"yield return {code}");
                    break;
                case TokenType.RightContentDelimiter:
                    return;
                default:
                    throw new SyntaxException(); // TODO: add specific information about position and kind of the error
            }
        }
    }

    private void EvaluateComment(IEnumerator<Token> tokenEnumerator, StringBuilder codeBuilder)
    {
        while (tokenEnumerator.MoveNext())
        {
            var token = tokenEnumerator.Current;
            switch (token.TokenType)
            {
                case TokenType.Literal:
                    // do nothing
                    break;
                case TokenType.RightCommentDelimiter:
                    return;
                default:
                    throw new SyntaxException(); // TODO: add specific information about position and kind of the error
            }
        }
    }
}