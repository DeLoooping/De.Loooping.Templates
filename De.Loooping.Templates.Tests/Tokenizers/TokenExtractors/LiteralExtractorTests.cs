using De.Loooping.Templates.Core.Tokenizers;
using De.Loooping.Templates.Core.Tokenizers.TokenExtractors;

namespace De.Loooping.Templates.Tests.Tokenizers.TokenExtractors;

public class LiteralExtractorTests
{
    [Fact(DisplayName = $"{nameof(LiteralExtractor)} extracts a literal until its delimiter")]
    public void LiteralExtractorExtractsUntilDelimiter()
    {
        // setup
        string toBeScanned = "preambel literal }end";
        var extractor = new LiteralExtractor(toBeScanned, ["}"]);

        // act
        bool isExtracted = extractor.TryExtract(8, out var token);

        // verify
        Assert.True(isExtracted);
        Assert.NotNull(token);
        Assert.Equal(TokenType.Literal, token.TokenType);
        Assert.Equal(" literal ", token.Value);
        Assert.Equal(9, token.CharactersConsumed);
        Assert.Equal(8, token.StartIndex);
    }
    
    [Fact(DisplayName = $"{nameof(LiteralExtractor)} extracts a literal until the first delimiter")]
    public void LiteralExtractorExtractsLazyUntilDelimiter()
    {
        // setup
        string toBeScanned = "preambel literal }{ another literal }end";
        var extractor = new LiteralExtractor(toBeScanned, ["}"]);

        // act
        bool isExtracted = extractor.TryExtract(8, out var token);

        // verify
        Assert.True(isExtracted);
        Assert.NotNull(token);
        Assert.Equal(TokenType.Literal, token.TokenType);
        Assert.Equal(" literal ", token.Value);
        Assert.Equal(9, token.CharactersConsumed);
        Assert.Equal(8, token.StartIndex);
    }
    
    [Fact(DisplayName = $"{nameof(LiteralExtractor)} extracts a literal at end of source")]
    public void LiteralExtractorExtractsLiteralAtEndOfSource()
    {
        // setup
        string toBeScanned = "preambel literal ";
        var extractor = new LiteralExtractor(toBeScanned, ["}"]);

        // act
        bool isExtracted = extractor.TryExtract(8, out var token);

        // verify
        Assert.True(isExtracted);
        Assert.NotNull(token);
        Assert.Equal(TokenType.Literal, token.TokenType);
        Assert.Equal(" literal ", token.Value);
        Assert.Equal(9, token.CharactersConsumed);
        Assert.Equal(8, token.StartIndex);
    }
}