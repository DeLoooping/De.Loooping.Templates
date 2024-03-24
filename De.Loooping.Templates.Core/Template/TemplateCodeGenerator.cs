using System.Text.RegularExpressions;
using De.Loooping.Templates.Core.CodeMapping;
using De.Loooping.Templates.Core.Diagnostic;
using De.Loooping.Templates.Core.Template.CustomBlocks;
using De.Loooping.Templates.Core.Tokenizers;
using De.Loooping.Templates.Core.Tools;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace De.Loooping.Templates.Core.Template;

internal class TemplateCodeGenerator
{
    private const string _COMPILATION_TEMPLATE_CLASS_NAME = "Template";
    private const string _COMPILATION_TEMPLATE_ENUMERABLE_METHOD_NAME = "Evaluate";
    
    private readonly Regex _whitespaceUntilEndRegex = new Regex(@"\G\s*\z", RegexOptions.Compiled);
    private readonly Regex _doubleQuoteSequenceRegex = new Regex("\"*", RegexOptions.Compiled);
    
    private readonly string _namespaceName;
    private readonly string _className;
    private readonly string _methodName;
    private readonly ParseOptions _parseOptions;
    private readonly Dictionary<string, ICustomBlock> _customBlocks;

    private enum ContentState
    {
        Code,
        Format
    }

    private enum CustomBlockState
    {
        Identifier,
        Content,
        Done
    }

    public TemplateCodeGenerator(string namespaceName, string className, string methodName, ParseOptions parseOptions, Dictionary<string, ICustomBlock> customBlocks)
    {
        _namespaceName = namespaceName;
        _className = className;
        _methodName = methodName;
        _parseOptions = parseOptions;
        _customBlocks = customBlocks;
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

        codeMapper.AddGeneratedCodeFromNil($$"""
                                             namespace {{_namespaceName}};

                                             public class {{_className}}
                                             {
                                             	{{GetFullName(typeof(CodeMapper))}} _codeMapper;
                                             
                                             	{{GetFullName(typeof(Regex))}} _lineFinderRegex = new {{GetFullName(typeof(Regex))}}("at {{_namespaceName}}.{{_COMPILATION_TEMPLATE_CLASS_NAME}}.{{_COMPILATION_TEMPLATE_ENUMERABLE_METHOD_NAME}}\\(.*?:line (?<line>\\d+)", {{GetFullName(typeof(RegexOptions))}}.{{nameof(RegexOptions.Compiled)}});
                                             
                                             	public {{_className}}({{GetFullName(typeof(CodeMapper))}} codeMapper)
                                             	{
                                             		_codeMapper = codeMapper;
                                             	}
                                             
                                             	public string {{_methodName}}({{String.Join(", ", parametersWithType)}})
                                             	{
                                             		try {
                                             			var result = {{_COMPILATION_TEMPLATE_CLASS_NAME}}.{{_COMPILATION_TEMPLATE_ENUMERABLE_METHOD_NAME}}({{String.Join(", ", parameterNames)}});
                                             			return String.Concat(result);
                                             		}
                                             		catch (Exception e)
                                             		{
                                             			var lineMatch = _lineFinderRegex.Match(e.StackTrace);
                                             			int line = lineMatch.Success ? Int32.Parse(lineMatch.Groups["line"].Value) : 1;
                                             			var location = _codeMapper.{{nameof(CodeMapper.GetGeneratingCodeLocation)}}(new {{GetFullName(typeof(CodeLocation))}}(line,1));
                                             			throw new {{GetFullName(typeof(RuntimeErrorException))}}($"A runtime error occured: {e.Message}", location, e);
                                             		}
                                             	}
                                             }

                                             internal static class {{_COMPILATION_TEMPLATE_CLASS_NAME}}
                                             {
                                             	public static {{GetFullName(typeof(IEnumerable<string>))}} {{_COMPILATION_TEMPLATE_ENUMERABLE_METHOD_NAME}}({{String.Join(", ", parametersWithType)}})
                                             	{
                                             
                                             """);

        // translate template code
        int bodyStart = codeMapper.GeneratedCodeLength;
        EvaluateRoot(enumerator, codeMapper);
        int bodyEnd = codeMapper.GeneratedCodeLength;
        
        codeMapper.AddGeneratedCodeFromNil($$"""
                                             	}
                                             }
                                             """);
        
        string code = codeMapper.GeneratedCode;
        string body = code.Substring(bodyStart, bodyEnd - bodyStart);
        AssureCodeIsOneStatementBlock(body, codeMapper, bodyStart);

        return code;
    }

    private void EvaluateRoot(IEnumerator<Token> tokenEnumerator, CodeMapper codeMapper)
    {
        while (tokenEnumerator.MoveNext())
        {
            var token = tokenEnumerator.Current;
            switch (token.TokenType)
            {
                case TokenType.Literal:
                    string literal = SymbolDisplay.FormatLiteral(token.Value, true); // can return string literal ("") or verbatim string (@"")
                    bool isVerbatimString = literal.Substring(0, 2) == "@\"";
                    
                    string quotePrefix;
                    EscapeSequenceMatcher escapeSequenceMatcher;
                    if (isVerbatimString)
                    {
                        // verbatim string
                        literal = literal.Substring(2, literal.Length - 3); // remove verbatim quotes @"..."
                        quotePrefix = "@\"";
                        escapeSequenceMatcher = CodeMapper.VerbatimStringEscapeSequenceMatcher;
                    }
                    else
                    {
                        // string literal
                        literal = literal.Substring(1, literal.Length - 2); // remove quotes "..."
                        quotePrefix = "\"";
                        escapeSequenceMatcher = CodeMapper.StringLiteralEscapeSequenceMatcher;
                    }
                    
                    codeMapper.AddGeneratedCodeFromNil($"yield return {quotePrefix}");
                    codeMapper.AddEscapedUserProvidedCode(literal, escapeSequenceMatcher);
                    codeMapper.AddGeneratedCodeFromNil("\";\n"); // suffix is the same for verbatim string and string literal
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
                case TokenType.LeftCustomBlockDelimiter:
                    codeMapper.AddNilGeneratingCode(token.Value);
                    EvaluateCustomBlock(tokenEnumerator, codeMapper);
                    break;
                default:
                    throw UnexpectedTokenException(token, codeMapper);
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
                    break;
                case TokenType.RightStatementDelimiter:
                    codeMapper.AddNilGeneratingCode(token.Value);
                    return;
                default:
                    throw UnexpectedTokenException(token, codeMapper);
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
                        throw UnexpectedTokenException(token, codeMapper);
                    }
                    
                    code = token.Value;
                    if (String.IsNullOrWhiteSpace(code))
                    {
                        throw UnexpectedTokenException(token, codeMapper);
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
                    if (format != null || currentState != ContentState.Format)
                    {
                        throw UnexpectedTokenException(token, codeMapper);
                    }
                    
                    format = token.Value
                        .Replace("{", "{{")
                        .Replace("}", "}}");
                    codeMapper.AddEscapedUserProvidedCode(format, CodeMapper.BracketEscapeSequenceMatcher);
                    
                    break;
                case TokenType.RightContentDelimiter:
                    if (String.IsNullOrEmpty(code))
                    {
                        throw UnexpectedTokenException(token, codeMapper);
                    }
                    
                    codeMapper.AddNilGeneratingCode(token.Value);
                    codeMapper.AddGeneratedCodeFromNil("}\";\n");
                    
                    return;
                default:
                    throw UnexpectedTokenException(token, codeMapper);
            }
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
                    codeMapper.AddNilGeneratingCode(token.Value);
                    break;
                case TokenType.RightCommentDelimiter:
                    codeMapper.AddNilGeneratingCode(token.Value);
                    return;
                default:
                    throw UnexpectedTokenException(token, codeMapper);
            }
        }
    }
    
    private void EvaluateCustomBlock(IEnumerator<Token> tokenEnumerator, CodeMapper codeMapper)
    {
        codeMapper.AddGeneratedCodeFromNil("yield return ");

        CustomBlockState currentState = CustomBlockState.Identifier;
        ICustomBlock? customBlock = null;
        while (tokenEnumerator.MoveNext())
        {
            var token = tokenEnumerator.Current;
            switch (token.TokenType)
            {
                case TokenType.Identifier:
                    if (customBlock != null || currentState != CustomBlockState.Identifier)
                    {
                        throw UnexpectedTokenException(token, codeMapper);
                    }
                    
                    string identifier = token.Value.Trim();
                    if (String.IsNullOrWhiteSpace(identifier))
                    {
                        throw UnexpectedTokenException(token, codeMapper);
                    }

                    if (!_customBlocks.TryGetValue(identifier, out customBlock))
                    {
                        var location = codeMapper.GetGeneratingCodeLocation(codeMapper.GeneratingCodeLength);
                        var error = new Error($"Unknown custom block identifier '{identifier}'", location);
                        throw new SyntaxErrorException($"Unknown custom block identifier", new[] { error });
                    }

                    codeMapper.AddNilGeneratingCode(token.Value);
                    break;
                case TokenType.CustomBlockIdentifierDelimiter:
                    if (currentState != CustomBlockState.Identifier)
                    {
                        throw UnexpectedTokenException(token, codeMapper);
                    }
                    
                    currentState = CustomBlockState.Content;
                    codeMapper.AddNilGeneratingCode(token.Value);
                    break;
                case TokenType.Literal:
                    if (customBlock == null || currentState != CustomBlockState.Content)
                    {
                        throw UnexpectedTokenException(token, codeMapper);
                    }

                    string content = token.Value;
                    string translation = customBlock.Evaluate(content);
                    string stringDelimiter = GetSaveRawStringDelimiter(translation);
                    codeMapper.AddGeneratedCodeFromNil($"\n{stringDelimiter}\n");
                    codeMapper.AddTranslatedCode(content, translation);
                    codeMapper.AddGeneratedCodeFromNil($"\n{stringDelimiter}");

                    currentState = CustomBlockState.Done;
                    break;
                case TokenType.RightCustomBlockDelimiter:
                    if (currentState != CustomBlockState.Done)
                    {
                        throw UnexpectedTokenException(token, codeMapper);
                    }
                    
                    codeMapper.AddNilGeneratingCode(token.Value);
                    codeMapper.AddGeneratedCodeFromNil(";\n");
                    
                    return;
                default:
                    throw UnexpectedTokenException(token, codeMapper);
            }
        }
    }

    private string GetSaveRawStringDelimiter(string translation)
    {
        int longestSequence = Math.Max(_doubleQuoteSequenceRegex.Matches(translation).Select(m => m.Length).Max(), 3);
        return String.Concat(Enumerable.Repeat("\"", longestSequence + 1));
    }

    private static SyntaxErrorException UnexpectedTokenException(Token token, CodeMapper codeMapper)
    {
        return new SyntaxErrorException("Unexpected token", new[] { 
            new Error($"Unexpected token {token.TokenType} with value '{token.Value}'",
                codeMapper.GetGeneratingCodeLocation(codeMapper.GeneratedCodeLength))
        });
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
            throw new SyntaxErrorException("Content element does not contain an expression",
                expressionSyntaxErrors.Select(e=>e.ToError(codeMapper, startPosition)));
        }
    }

    private void AssureCodeIsOneStatementBlock(string code, CodeMapper codeMapper, int startPosition)
    {
        // assure that code cannot not escape the method body

        int offset = 0;
        while (offset < code.Length)
        {
            if (_whitespaceUntilEndRegex.IsMatch(code, offset))
            {
                // end of code
                return;
            }
            
            var statementSyntax = SyntaxFactory.ParseStatement(code, offset, _parseOptions, false);
            var statementSyntaxDiagnostics = statementSyntax.GetDiagnostics();
            var statementSyntaxErrors = statementSyntaxDiagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();
            
            if (statementSyntax == null || statementSyntaxErrors.Any())
            {
                var errors = statementSyntaxErrors.Select(e =>
                {
                    int realStartPosition = startPosition + offset; // respect previous code
                    return e.ToError(codeMapper, realStartPosition);
                });
                throw new SyntaxErrorException("Code is not a statement block", errors);
            }

            offset += statementSyntax.FullSpan.Length;
        }
    }
}