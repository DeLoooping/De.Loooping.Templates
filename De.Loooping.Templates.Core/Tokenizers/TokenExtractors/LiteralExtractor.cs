using System.Text.RegularExpressions;

namespace De.Loooping.Templates.Core.Tokenizers.TokenExtractors;

internal class LiteralExtractor: AbstractTokenExtractor
{
    private readonly Regex _literalRegex;
    
    public LiteralExtractor(string toBeScanned, IEnumerable<string> rightDelimiters)
        : base(toBeScanned)
    {
        IEnumerable<string> escapedDelimiters = rightDelimiters.Select(rightDelimiter => Regex.Escape(rightDelimiter));
        string rightDelimitersExpression = String.Join("|", escapedDelimiters);
        _literalRegex = new Regex($"\\G(?<value>(.|\n)*?)({rightDelimitersExpression}|$)", RegexOptions.Compiled);
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

        token = new Token()
        {
            TokenType = TokenType.Literal,
            Value = value,
            CharactersConsumed = value.Length,
            StartIndex = startIndex
        };
        return true;
    }
}