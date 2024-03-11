using System.Text.RegularExpressions;
using De.Loooping.Templates.Core.CodeMapping;
using De.Loooping.Templates.Core.Diagnostic;
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

        codeMapper = new CodeMapper();

        foreach (string u in usings)
        {
            codeMapper.AddGeneratedCodeFromNil($"using {u};\n");
        }

        codeMapper.AddGeneratedCodeFromNil($"namespace {_namespaceName};\n\n");
        codeMapper.AddGeneratedCodeFromNil($"public class {_className} {{\n");

        codeMapper.AddGeneratedCodeFromNil($"public static string {_methodName}({String.Join(", ", parametersWithType)})\n{{\n");
        codeMapper.AddGeneratedCodeFromNil($"   var result = {_COMPILATION_ENUMERABLE_METHOD}({String.Join(", ", parameterNames)});\n");
        codeMapper.AddGeneratedCodeFromNil($"   return String.Concat(result);\n");
        codeMapper.AddGeneratedCodeFromNil("}\n\n");
        
        codeMapper.AddGeneratedCodeFromNil($"private static {GetFullName(typeof(IEnumerable<string>))} {_COMPILATION_ENUMERABLE_METHOD}({String.Join(", ", parametersWithType)})\n{{\n");

        int bodyStart = codeMapper.GeneratedCodeLength;
        EvaluateRoot(enumerator, codeMapper);
        int bodyEnd = codeMapper.GeneratedCodeLength;
        
        codeMapper.AddGeneratedCodeFromNil("}\n}");
        
        string code = codeMapper.GeneratedCode;
        string body = code.Substring(bodyStart, bodyEnd - bodyStart);
        AssureCodeIsOneStatementBlock(body, codeMapper, bodyStart);

        return code;
    }

    private static void AssureCodeIsOneStatementBlock(string code, CodeMapper codeMapper, int startPosition)
    {
        // assure that code cannot not escape the method body
        var statementSyntax = SyntaxFactory.ParseStatement($"{{{code}}}");
        var statementSyntaxDiagnostics = statementSyntax.GetDiagnostics();
        var statementSyntaxErrors = statementSyntaxDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        if (!(statementSyntax is BlockSyntax) || statementSyntax.Span != statementSyntax.FullSpan || statementSyntaxErrors.Any())
        {
            var errors = statementSyntaxErrors.Select(e=>e.ToError(codeMapper, startPosition));
            throw new SyntaxErrorException("Code is not a statement block", errors);
        }
    }

    private void EvaluateRoot(IEnumerator<Token> tokenEnumerator, CodeMapper codeMapper)
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
                    break;
                case TokenType.LeftCommentDelimiter:
                    codeMapper.AddNilGeneratingCode(token.Value);
                    EvaluateComment(tokenEnumerator, codeMapper);
                    break;
                case TokenType.LeftContentDelimiter:
                    codeMapper.AddNilGeneratingCode(token.Value);
                    EvaluateContent(tokenEnumerator, codeMapper);
                    break;
                case TokenType.LeftStatementDelimiter:
                    codeMapper.AddNilGeneratingCode(token.Value);
                    EvaluateStatement(tokenEnumerator, codeMapper);
                    break;
                default:
                    // TODO: add specific information about position and kind of the error
                    throw new SyntaxErrorException($"Unexpected token {token.TokenType} with value '{token.Value}'", []);
            }
        }
    }

    private void EvaluateStatement(IEnumerator<Token> tokenEnumerator, CodeMapper codeMapper)
    {
        while (tokenEnumerator.MoveNext())
        {
            var token = tokenEnumerator.Current;
            switch (token.TokenType)
            {
                case TokenType.CSharp:
                    string code = token.Value;
                    codeMapper.AddUserProvidedCode(code);
                    //codeBuilder.Append(code);
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

    private void EvaluateContent(IEnumerator<Token> tokenEnumerator, CodeMapper codeMapper)
    {
        codeMapper.AddGeneratedCodeFromNil("""yield return $"{""");

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

                    int basePosition = codeMapper.GeneratedCodeLength;
                    codeMapper.AddUserProvidedCode(code);
                    AssureCodeIsExpression(code, codeMapper, basePosition);

                    break;
                case TokenType.ContentFormatDelimiter:
                    
                    currentState = ContentState.Format;
                    codeMapper.AddNilGeneratingCode(token.Value);
                    codeMapper.AddGeneratedCodeFromNil(":");
                    break;
                case TokenType.Literal:
                    if (format != null || currentState != ContentState.Format || token.Value == null)
                    {
                        // TODO: add specific information about position and kind of the error
                        throw new SyntaxErrorException($"Unexpected token {token.TokenType} with value '{token.Value}'", []);
                    }
                    
                    format = token.Value
                        .Replace("{", "{{")
                        .Replace("}", "}}");
                    codeMapper.AddEscapedUserProvidedCode(format, _bracketEscaping);
                    
                    break;
                case TokenType.RightContentDelimiter:
                    if (String.IsNullOrEmpty(code))
                    {
                        // TODO: add specific information about position and kind of the error
                        throw new SyntaxErrorException($"Unexpected token {token.TokenType} with value '{token.Value}'", []);
                    }
                    
                    codeMapper.AddNilGeneratingCode(token.Value);
                    codeMapper.AddGeneratedCodeFromNil("}\";\n");
                    
                    return;
                default:
                    throw new SyntaxErrorException("Unknown token", []); // TODO: add specific information about position and kind of the error
            }
        }
    }

    private static void AssureCodeIsExpression(string code, CodeMapper codeMapper, int startPosition)
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
                expressionSyntaxErrors.Select(e=>e.ToError(codeMapper, startPosition)));
        }
    }

    private void EvaluateComment(IEnumerator<Token> tokenEnumerator, CodeMapper codeMapper)
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