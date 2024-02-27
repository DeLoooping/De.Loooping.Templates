using De.Loooping.Templates.Core.Tokenizers;
using De.Loooping.Templates.Core.Tokenizers.TokenExtractors;

namespace De.Loooping.Templates.Tests.Tokenizers.TokenExtractors;

public class DelimiterExtractorTests
{
    [Fact(DisplayName = $"{nameof(DelimiterExtractor)} extracts a delimiter")]
    public void DelimiterExtractorExtractsDelimiter()
    {
        // setup
        string toBeScanned = "preambel{{{middle{{{end";
        var extractor = new DelimiterExtractor(toBeScanned, "{{{", TokenType.LeftCommentDelimiter);

        // act
        bool isExtracted = extractor.TryExtract(8, out var token);

        // verify
        Assert.True(isExtracted);
        Assert.NotNull(token);
        Assert.Equal(TokenType.LeftCommentDelimiter, token.TokenType);
        Assert.Equal("{{{", token.Value);
        Assert.Equal(3, token.CharactersConsumed);
        Assert.Equal(8, token.StartIndex);
    }

    [Fact(DisplayName = $"{nameof(DelimiterExtractor)} extracts a delimiter at the end of the source string")]
    public void DelimiterExtractorExtractsDelimiterAtEndOfSource()
    {
        // setup
        string toBeScanned = "preambel{{{";
        var extractor = new DelimiterExtractor(toBeScanned, "{{{", TokenType.LeftCommentDelimiter);

        // act
        bool isExtracted = extractor.TryExtract(8, out var token);

        // verify
        Assert.True(isExtracted);
        Assert.NotNull(token);
        Assert.Equal(TokenType.LeftCommentDelimiter, token.TokenType);
        Assert.Equal("{{{", token.Value);
        Assert.Equal(3, token.CharactersConsumed);
        Assert.Equal(8, token.StartIndex);
    }

}