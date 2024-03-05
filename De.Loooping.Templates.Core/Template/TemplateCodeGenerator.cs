using System.Text;
using De.Loooping.Templates.Core.TemplateProcessors;
using De.Loooping.Templates.Core.Tokenizers;
using Microsoft.CodeAnalysis.CSharp;

namespace De.Loooping.Templates.Core.Template;

internal class TemplateCodeGenerator
{
    private enum ContentState
    {
        Code,
        Format
    }
    
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
                    // TODO: add specific information about position and kind of the error
                    throw new SyntaxErrorException($"Unexpected token {token.TokenType} with value '{token.Value}'", []);
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
                    // TODO: add specific information about position and kind of the error
                    throw new SyntaxErrorException($"Unexpected token {token.TokenType} with value '{token.Value}'", []);
            }
        }
    }

    private void EvaluateContent(IEnumerator<Token> tokenEnumerator, StringBuilder codeBuilder)
    {
        ContentState currentState = ContentState.Code;
        string? code = null;
        string? format = null;
        while (tokenEnumerator.MoveNext())
        {
            var token = tokenEnumerator.Current;
            switch (token.TokenType)
            {
                case TokenType.CSharp:
                    if (code != null || currentState != ContentState.Code)
                    {
                        // TODO: add specific information about position and kind of the error
                        throw new SyntaxErrorException($"Unexpected token {token.TokenType} with value '{token.Value}'", []);
                    }
                    
                    code = token.Value.Trim();
                    if (code.Length == 0)
                    {
                        // TODO: add specific information about position and kind of the error
                        throw new SyntaxErrorException($"Unexpected token {token.TokenType} with value '{token.Value}'", []);
                    }
                    
                    //TODO: check that code is expression

                    /*if (code.Last() != ';')
                    {
                        code += ";";
                    }*/

                    break;
                case TokenType.ContentFormatDelimiter:
                    currentState = ContentState.Format;
                    break;
                case TokenType.Literal:
                    if (format != null || currentState != ContentState.Format || token.Value == null)
                    {
                        // TODO: add specific information about position and kind of the error
                        throw new SyntaxErrorException($"Unexpected token {token.TokenType} with value '{token.Value}'", []);
                    }
                    format = token.Value;
                    break;
                case TokenType.RightContentDelimiter:
                    if (String.IsNullOrEmpty(code))
                    {
                        // TODO: add specific information about position and kind of the error
                        throw new SyntaxErrorException($"Unexpected token {token.TokenType} with value '{token.Value}'", []);
                    }

                    if (format != null)
                    {
                        // escape curly braces for use inside interpolated string
                        format = format
                            .Replace("{", "{{")
                            .Replace("}", "}}");
                        
                        codeBuilder.AppendLine($"yield return $\"{{{code}:{format}}}\";");
                    }
                    else
                    {
                        codeBuilder.AppendLine($"yield return $\"{{{code}}}\";");
                    }

                    return;
                default:
                    throw new SyntaxErrorException("Unknown token", []); // TODO: add specific information about position and kind of the error
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
                    // TODO: add specific information about position and kind of the error
                    throw new SyntaxErrorException($"Unexpected token {token.TokenType} with value '{token.Value}'", []);
            }
        }
    }
}