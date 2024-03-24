using De.Loooping.Templates.Core.Tokenizers;
using De.Loooping.Templates.Core.Tokenizers.TokenExtractors;
using Microsoft.CodeAnalysis.CSharp;

namespace De.Loooping.Templates.Core.Tests.Tokenizers.TokenExtractors;

public class CSharpExtractorTests
{
    [Fact(DisplayName = $"{nameof(CSharpExtractor)} can extract regular string with escaped \" and end delimiter" )]
    public void CSharpExtractorCanExtractCodeWithEscapedDoubleQuotesAndEndDelimiter()
    {
        // setup
        string toBeScanned = """preamble string a = "\"}\""; }""";
        var extractor = new CSharpExtractor(toBeScanned, ["}"], new CSharpParseOptions(LanguageVersion.CSharp11));
        
        // act
        bool isExtracted = extractor.TryExtract(8, out var token);

        // verify
        Assert.True(isExtracted);
        Assert.NotNull(token);
        Assert.Equal(TokenType.CSharp, token.TokenType);
        Assert.Equal(""" string a = "\"}\""; """, token.Value);
        Assert.Equal(21, token.CharactersConsumed);
        Assert.Equal(8, token.StartIndex);
    }

    [Fact(DisplayName = $"{nameof(CSharpExtractor)} does not extract if it starts at invalid character")]
    public void CSharpRegularStringExtractorDoesNotExtractIfInvalidCharacter()
    {
        // setup
        string toBeScanned = """preamble°epilogue""";
        var extractor = new CSharpExtractor(toBeScanned, ["}"], new CSharpParseOptions(LanguageVersion.CSharp11));
        
        // act
        bool isExtracted = extractor.TryExtract(8, out var token);

        // verify
        Assert.False(isExtracted);
        Assert.Null(token);
    }

    [Fact(DisplayName = $"{nameof(CSharpExtractor)} does extract if code contains newline")]
    public void CSharpRegularStringExtractorDoesExtractIfStringContainsNewline()
    {
        // setup
        string toBeScanned = "statement1();\nstatement2();";
        var extractor = new CSharpExtractor(toBeScanned, ["}"], new CSharpParseOptions(LanguageVersion.CSharp11));
        
        // act
        bool isExtracted = extractor.TryExtract(0, out var token);

        // verify
        Assert.True(isExtracted);
        Assert.NotNull(token);
        Assert.Equal(TokenType.CSharp, token.TokenType);
        Assert.Equal("statement1();\nstatement2();", token.Value);
        Assert.Equal(27, token.CharactersConsumed);
        Assert.Equal(0, token.StartIndex);
    }

    [Fact(DisplayName = $"{nameof(CSharpExtractor)} does extract if code contains newline and spaces at end")]
    public void CSharpRegularStringExtractorDoesExtractIfStringContainsNewlineAndSpacesAtEnd()
    {
        // setup
        string toBeScanned = "statement1();\nstatement2();\n  }";
        var extractor = new CSharpExtractor(toBeScanned, ["}"], new CSharpParseOptions(LanguageVersion.CSharp11));
        
        // act
        bool isExtracted = extractor.TryExtract(0, out var token);

        // verify
        Assert.True(isExtracted);
        Assert.NotNull(token);
        Assert.Equal(TokenType.CSharp, token.TokenType);
        Assert.Equal("statement1();\nstatement2();\n  ", token.Value);
        Assert.Equal(30, token.CharactersConsumed);
        Assert.Equal(0, token.StartIndex);
    }
}