using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace De.Loooping.Templates.Core.Tokenizers.TokenExtractors;

internal class CSharpExtractor: AbstractTokenExtractor
{
    #region static fields
    private static readonly Assembly _MICROSOFT_CODE_ANALYSIS_ASSEMBLY;
    private static readonly Assembly _MICROSOFT_CODE_ANALYSIS_CSHARP_ASSEMBLY;
    
    private static readonly Type _GREEN_NODE_TYPE;
    private static readonly PropertyInfo _GREEN_NODE_FULL_WIDTH;
    
    private static readonly Type _DIRECTIVE_STACK_TYPE;
    private static readonly object _DIRECTIVE_STACK_EMPTY;

    private static readonly Type _LEXER_TYPE;
    private static readonly ConstructorInfo _LEXER_CONSTRUCTOR;

    private static readonly MethodInfo _LEXER_LEX_METHOD;
    private static readonly MethodInfo _LEXER_RESET_METHOD;

    private static readonly object _LEXER_MODE_SYNTAX;

    private static readonly PropertyInfo _CSHARP_SYNTAX_NODE_KIND_PROPERTY;
    
    static CSharpExtractor()
    {
        // HINT: the C# lexer is not public, so we have to work via reflection
        
        _MICROSOFT_CODE_ANALYSIS_ASSEMBLY = typeof(SourceCodeKind).Assembly;
        _MICROSOFT_CODE_ANALYSIS_CSHARP_ASSEMBLY = typeof(LanguageVersion).Assembly;
        
        _GREEN_NODE_TYPE = GetType(_MICROSOFT_CODE_ANALYSIS_ASSEMBLY, "GreenNode");
        _GREEN_NODE_FULL_WIDTH = _GREEN_NODE_TYPE.GetProperty("FullWidth")!;
        
        _DIRECTIVE_STACK_TYPE = GetType(_MICROSOFT_CODE_ANALYSIS_CSHARP_ASSEMBLY, "DirectiveStack");
        FieldInfo directiveStackEmptyField = _DIRECTIVE_STACK_TYPE.GetField("Empty")!;
        _DIRECTIVE_STACK_EMPTY = directiveStackEmptyField.GetValue(_DIRECTIVE_STACK_TYPE)!;
        
        _LEXER_TYPE = GetType(_MICROSOFT_CODE_ANALYSIS_CSHARP_ASSEMBLY, "Lexer");
        _LEXER_CONSTRUCTOR = _LEXER_TYPE.GetConstructor(new[] { typeof(SourceText), typeof(CSharpParseOptions), typeof(bool), typeof(bool) })!;
        
        Type lexerMode = GetType(_MICROSOFT_CODE_ANALYSIS_CSHARP_ASSEMBLY, "LexerMode");
        _LEXER_MODE_SYNTAX = lexerMode.GetField("Syntax")!.GetValue(lexerMode)!;
        
        _LEXER_LEX_METHOD = _LEXER_TYPE.GetMethod("Lex", [lexerMode])!;
        _LEXER_RESET_METHOD = _LEXER_TYPE.GetMethod("Reset", [typeof(int), _DIRECTIVE_STACK_TYPE])!;

        Type cSharpSyntaxNodeType = GetType(_MICROSOFT_CODE_ANALYSIS_CSHARP_ASSEMBLY, "CSharpSyntaxNode", "Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax");
        _CSHARP_SYNTAX_NODE_KIND_PROPERTY = cSharpSyntaxNodeType.GetProperty("Kind")!;
    }
    #endregion

    private readonly List<string> _rightDelimiters;
    private readonly Regex _rightDelimiterSearchRegex;
    private readonly CSharpParseOptions _parseOptions;

    public CSharpExtractor(string toBeScanned, IEnumerable<string> rightDelimiters, CSharpParseOptions parseOptions) : base(toBeScanned)
    {
        _parseOptions = parseOptions;
        _rightDelimiters = rightDelimiters.ToList();

        var escapedDelimiters = _rightDelimiters.Select(Regex.Escape);
        _rightDelimiterSearchRegex = new Regex($"\\G(?<whitespace>\\s*)(?<delimiter>({String.Join("|", escapedDelimiters.Concat(["$"]))}))");
    }

    public override bool TryExtract(int startIndex, out Token? token)
    {
        var sourceText = SourceText.From(ToBeScanned, Encoding.Default);
        var lexer = _LEXER_CONSTRUCTOR.Invoke(new object[] { sourceText, _parseOptions, false, false })!;

        int index = startIndex;
        do
        {
            var rightDelimiterMatch = _rightDelimiterSearchRegex.Match(ToBeScanned, index);
            if (rightDelimiterMatch.Success)
            {
                // right delimiter or end of string found
                string whitespace = rightDelimiterMatch.Groups["whitespace"].Value;
                index += whitespace.Length;
                break;
            }

            _LEXER_RESET_METHOD.Invoke(lexer, [index, _DIRECTIVE_STACK_EMPTY]);
            object syntaxToken = _LEXER_LEX_METHOD.Invoke(lexer, [_LEXER_MODE_SYNTAX])!;
            int fullWidth = GetFullWidth(syntaxToken);
            if (fullWidth == 0 || _CSHARP_SYNTAX_NODE_KIND_PROPERTY.GetValue(syntaxToken)!.Equals(SyntaxKind.BadToken))
            {
                break;
            }

            index += fullWidth;
        } while (true);

        if (startIndex == index)
        {
            token = null;
            return false;
        }

        int charactersConsumed = index - startIndex;
        token = new Token()
        {
            TokenType = TokenType.CSharp,
            Value = ToBeScanned.Substring(startIndex, charactersConsumed),
            StartIndex = startIndex,
            CharactersConsumed = charactersConsumed
        };
        return true;
    }

    private static int GetFullWidth(object syntaxToken)
    {
        return (int)_GREEN_NODE_FULL_WIDTH.GetValue(syntaxToken)!;
    }

    private static Type GetType(Assembly assembly, string typeName, string? inNamespace = null)
    {
        return assembly.GetTypes().Single(t => t.Name == typeName && (inNamespace == null || t.Namespace == inNamespace));
    }
}