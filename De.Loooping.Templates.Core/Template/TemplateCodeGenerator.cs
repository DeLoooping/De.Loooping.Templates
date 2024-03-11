using System.Text;
using System.Text.RegularExpressions;
using De.Loooping.Templates.Core.CodeMapping;
using De.Loooping.Templates.Core.TemplateProcessors;
using De.Loooping.Templates.Core.Tokenizers;
using De.Loooping.Templates.Core.Tools;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace De.Loooping.Templates.Core.Template;

internal class TemplateCodeGenerator
{
    private const string _COMPILATION_ENUMERABLE_METHOD = "GetParts";

    private readonly Regex _backslashEscaping = new Regex(@"\G(?<escape>\\)(?<escaped>.)", RegexOptions.Compiled);
    private readonly Regex _bracketEscaping = new Regex(@"\G((?<escape>\{)(?<escaped>\{)|(?<escape>\})(?<escaped>\}))", RegexOptions.Compiled);
    
    private readonly string _namespaceName;
    private readonly string _className;
    private readonly string _methodName;

    private enum ContentState
    {
        Code,
        Format
    }

    public TemplateCodeGenerator(string namespaceName, string className, string methodName)
    {
        _namespaceName = namespaceName;
        _className = className;
        _methodName = methodName;
    }
    
    private string GetFullName(Type type)
    {
        return TypeNameResolver.GetFullName(type);
    }
    
    public string Generate(IEnumerable<Token> tokens, IEnumerable<KeyValuePair<string,Type>> parameters, IEnumerable<string> usings, out CodeMapper codeMapper)
    {
        StringBuilder sb = new();
        IEnumerator<Token> enumerator = tokens.GetEnumerator();

        List<string> parameterNames = new();
        List<string> parametersWithType = new();
        foreach (var kvp in parameters)
        {
            string name = kvp.Key;
            Type type = kvp.Value;
            parameterNames.Add(name);
            parametersWithType.Add($"{GetFullName(type)} {name}");
        }

        foreach (string u in usings)
        {
            sb.AppendLine($"using {u};");
        }

        sb.AppendLine($"namespace {_namespaceName};\n");
        sb.AppendLine($"public class {_className} {{");

        sb.AppendLine($"public static string {_methodName}({String.Join(", ", parametersWithType)}) {{");
        sb.AppendLine($"   var result = {_COMPILATION_ENUMERABLE_METHOD}({String.Join(", ", parameterNames)});");
        sb.AppendLine($"   return String.Concat(result);");
        sb.AppendLine("}");
        
        sb.AppendLine($"private static {GetFullName(typeof(IEnumerable<string>))} {_COMPILATION_ENUMERABLE_METHOD}({String.Join(", ", parametersWithType)})\n{{");
        
        codeMapper = new CodeMapper();
        StringBuilder bodyStringBuilder = new();
        EvaluateRoot(enumerator, bodyStringBuilder, codeMapper);
        string body = bodyStringBuilder.ToString();        
        AssureCodeIsOneStatementBlock(body);
        sb.AppendLine(body);
        
        sb.AppendLine("}\n}");

        string code = sb.ToString();

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

    private void EvaluateRoot(IEnumerator<Token> tokenEnumerator, StringBuilder codeBuilder, CodeMapper codeMapper)
    {
        while (tokenEnumerator.MoveNext())
        {
            var token = tokenEnumerator.Current;
            switch (token.TokenType)
            {
                case TokenType.Literal:
                    string literal = SymbolDisplay.FormatLiteral(token.Value, false);
                    codeMapper.AddGeneratedCodeFromNil("yield return \"");
                    codeMapper.AddEscapedUserProvidedCode(literal, _backslashEscaping);
                    codeMapper.AddGeneratedCodeFromNil("\";\n");
                    codeBuilder.Append($"yield return \"{literal}\";\n");
                    break;
                case TokenType.LeftCommentDelimiter:
                    codeMapper.AddNilGeneratingCode(token.Value);
                    EvaluateComment(tokenEnumerator, codeBuilder, codeMapper);
                    break;
                case TokenType.LeftContentDelimiter:
                    codeMapper.AddNilGeneratingCode(token.Value);
                    EvaluateContent(tokenEnumerator, codeBuilder, codeMapper);
                    break;
                case TokenType.LeftStatementDelimiter:
                    codeMapper.AddNilGeneratingCode(token.Value);
                    EvaluateStatement(tokenEnumerator, codeBuilder, codeMapper);
                    break;
                default:
                    // TODO: add specific information about position and kind of the error
                    throw new SyntaxErrorException($"Unexpected token {token.TokenType} with value '{token.Value}'", []);
            }
        }
    }

    private void EvaluateStatement(IEnumerator<Token> tokenEnumerator, StringBuilder codeBuilder, CodeMapper codeMapper)
    {
        while (tokenEnumerator.MoveNext())
        {
            var token = tokenEnumerator.Current;
            switch (token.TokenType)
            {
                case TokenType.CSharp:
                    string code = $"{token.Value}\n";
                    codeMapper.AddUserProvidedCode(code);
                    codeBuilder.Append(code);
                    break;
                case TokenType.RightStatementDelimiter:
                    codeMapper.AddNilGeneratingCode(token.Value);
                    return;
                default:
                    // TODO: add specific information about position and kind of the error
                    throw new SyntaxErrorException($"Unexpected token {token.TokenType} with value '{token.Value}'", []);
            }
        }
    }

    private void EvaluateContent(IEnumerator<Token> tokenEnumerator, StringBuilder codeBuilder, CodeMapper codeMapper)
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
                    
                    code = token.Value;
                    if (String.IsNullOrWhiteSpace(code))
                    {
                        // TODO: add specific information about position and kind of the error
                        throw new SyntaxErrorException($"Unexpected token {token.TokenType} with value '{code}'", []);
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
                        
                        codeMapper.AddGeneratedCodeFromNil("yield return $\\\"{{");
                        codeMapper.AddUserProvidedCode($"{code}:{format}");
                        codeMapper.AddGeneratedCodeFromNil("\";\n");
                        codeBuilder.Append($"yield return $\"{{{code}:{format}}}\";\n");
                    }
                    else
                    {
                        codeMapper.AddGeneratedCodeFromNil("yield return $\\\"{{");
                        codeMapper.AddUserProvidedCode(code);
                        codeMapper.AddGeneratedCodeFromNil("\";\n");
                        codeBuilder.Append($"yield return $\"{{{code}}}\";\n");
                    }

                    codeMapper.AddNilGeneratingCode(token.Value);

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

    private void EvaluateComment(IEnumerator<Token> tokenEnumerator, StringBuilder codeBuilder, CodeMapper codeMapper)
    {
        while (tokenEnumerator.MoveNext())
        {
            var token = tokenEnumerator.Current;
            switch (token.TokenType)
            {
                case TokenType.Literal:
                    // do nothing
                    codeMapper.AddNilGeneratingCode(token.Value);
                    break;
                case TokenType.RightCommentDelimiter:
                    codeMapper.AddNilGeneratingCode(token.Value);
                    return;
                default:
                    // TODO: add specific information about position and kind of the error
                    throw new SyntaxErrorException($"Unexpected token {token.TokenType} with value '{token.Value}'", []);
            }
        }
    }
}