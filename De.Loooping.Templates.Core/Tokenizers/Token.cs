namespace De.Loooping.Templates.Core.Tokenizers;

internal class Token
{
    public required TokenType TokenType { get; init; }
    public required string Value { get; init; }
    public required int StartIndex { get; init; }
    public required int CharactersConsumed { get; init; }
}