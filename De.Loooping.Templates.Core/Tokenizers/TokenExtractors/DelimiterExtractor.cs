namespace De.Loooping.Templates.Core.Tokenizers.TokenExtractors;

internal class DelimiterExtractor: AbstractTokenExtractor
{
    private readonly string _delimiter;
    private readonly TokenType _tokenType;

    public DelimiterExtractor(string toBeScanned, string delimiter, TokenType tokenType)
        : base(toBeScanned)
    {
        _delimiter = delimiter;
        _tokenType = tokenType;
    }

    public override bool TryExtract(int startIndex, out Token? token)
    {
        if (startIndex + _delimiter.Length > ToBeScanned.Length)
        {
            token = null;
            return false;
        }
        
        string substring = ToBeScanned.Substring(startIndex, _delimiter.Length);
        if (substring != _delimiter)
        {
            token = null;
            return false;
        }

        token = new Token()
        {
            TokenType = _tokenType,
            Value = substring,
            StartIndex = startIndex,
            CharactersConsumed = substring.Length
        };
        return true;
    }
}