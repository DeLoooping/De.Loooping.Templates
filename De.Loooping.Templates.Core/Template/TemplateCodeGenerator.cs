using System.Text;
using De.Loooping.Templates.Core.TemplateProcessors;
using De.Loooping.Templates.Core.Tokenizers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        string code = sb.ToString();
        
        AssureCodeIsOneStatementBlock(code);

        return code;
    }

    private static void AssureCodeIsOneStatementBlock(string code)
    {
        // assure that code cannot not escape the method body
        var statementSyntax = SyntaxFactory.ParseStatement($"{{{code}}}");
        var statementSyntaxDiagnostics = statementSyntax.GetDiagnostics();
        var statementSyntaxErrors = statementSyntaxDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        if (!(statementSyntax is BlockSyntax) || statementSyntax.Span != statementSyntax.FullSpan || statementSyntaxErrors.Any())
        {
            // TODO: add specific information about position and kind of the error
            throw new SyntaxErrorException("Code is not a statement block",
                statementSyntaxErrors.Select(e=>e.GetMessage()));
        }
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

                    AssureCodeIsExpression(code);

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

    private static void AssureCodeIsExpression(string code)
    {
        // assure that content code is an expression
        ExpressionSyntax expressionSyntax = SyntaxFactory.ParseExpression(code);
        var expressionSyntaxDiagnostics = expressionSyntax.GetDiagnostics();
        var expressionSyntaxErrors = expressionSyntaxDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        if (expressionSyntaxErrors.Any())
        {
            // TODO: add specific information about position and kind of the error
            throw new SyntaxErrorException("Content element does not contain an expression",
                expressionSyntaxErrors.Select(e=>e.GetMessage()));
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