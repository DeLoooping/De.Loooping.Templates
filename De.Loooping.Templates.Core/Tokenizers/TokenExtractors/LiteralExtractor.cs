using System.Text.RegularExpressions;

namespace De.Loooping.Templates.Core.Tokenizers.TokenExtractors;

internal class LiteralExtractor: AbstractTokenExtractor
{
    private readonly TokenType _tokenType;
    private readonly Func<string, string> _valueTransformer;
    private readonly Regex _literalRegex;
    
    public LiteralExtractor(string toBeScanned, IEnumerable<string> rightDelimiters, TokenType tokenType = TokenType.Literal, Func<string, string>? valueTransformer = null)
        : base(toBeScanned)
    {
        _tokenType = tokenType;
        _valueTransformer = valueTransformer ?? (v => v);
        IEnumerable<string> escapedDelimiters = rightDelimiters.Select(rightDelimiter => Regex.Escape(rightDelimiter));
        string rightDelimitersExpression = String.Join("|", escapedDelimiters);
        _literalRegex = new Regex($"\\G(?<value>(.|\\n)*?)({rightDelimitersExpression}|\\z)", RegexOptions.Compiled);
    }

    public override bool TryExtract(int startIndex, out Token? token)
    {
        Match match = _literalRegex.Match(ToBeScanned, startIndex);
        if (!match.Success)
        {
            token = null;
            return false;
        }

        string value = match.Groups["value"].Value;
        if (value.Length == 0)
        {
            token = null;
            return false;
        }

        string finalValue = _valueTransformer(value);
        token = new Token()
        {
            TokenType = _tokenType,
            Value = finalValue,
            CharactersConsumed = value.Length,
            StartIndex = startIndex
        };
        return true;
    }
}