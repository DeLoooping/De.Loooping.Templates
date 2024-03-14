using De.Loooping.Templates.Core.CodeMapping;
using De.Loooping.Templates.Core.Diagnostic;

namespace De.Loooping.Templates.Core.Tests.CodeMapping;

public class CodeMapperTests
{
    [Fact(DisplayName = $"{nameof(CodeMapper)} returns correct location for one-liners")]
    public void CodeMapperReturnsCorrectLocationForOneLiners()
    {
        // setup
        CodeMapper mapper = new CodeMapper();
        mapper.AddGeneratedCodeFromNil("aa");
        mapper.AddUserProvidedCode("bb");
        mapper.AddGeneratedCodeFromNil("cc");

        // act
        CodeLocation originalLocation = mapper.GetGeneratingCodeLocation(new CodeLocation(1, 4));
        
        // verify
        Assert.Equal(1, originalLocation.Line);
        Assert.Equal(2, originalLocation.Column);
    }

    [Fact(DisplayName = $"{nameof(CodeMapper)} returns correct location for multiline code")]
    public void CodeMapperReturnsCorrectLocationForMultilineCode()
    {
        // setup
        CodeMapper mapper = new CodeMapper();
        mapper.AddGeneratedCodeFromNil("aa\n");
        mapper.AddUserProvidedCode("bb\n");
        mapper.AddGeneratedCodeFromNil("cc");

        // act
        CodeLocation originalLocation = mapper.GetGeneratingCodeLocation(new CodeLocation(2, 2));
        
        // verify
        Assert.Equal(1, originalLocation.Line);
        Assert.Equal(2, originalLocation.Column);
    }
    
    [Fact(DisplayName = $"{nameof(CodeMapper)} returns nearest location to the left of non-mappable internal code")]
    public void CodeMapperReturnsNearestLeftLocationForNonMappableCode()
    {
        // setup
        CodeMapper mapper = new CodeMapper();
        mapper.AddGeneratedCodeFromNil("aa\n");
        mapper.AddUserProvidedCode("bb\n");
        mapper.AddGeneratedCodeFromNil("cc");

        // act
        CodeLocation originalLocation = mapper.GetGeneratingCodeLocation(new CodeLocation(3, 2));
        
        // verify
        Assert.Equal(1, originalLocation.Line);
        Assert.Equal(3, originalLocation.Column);
    }
    
    [Fact(DisplayName = $"{nameof(CodeMapper)} returns correct location for backslash escaped code")]
    public void CodeMapperReturnsCorrectLocationForBackslashEscapedCode()
    {
        // setup
        CodeMapper mapper = new CodeMapper();
        mapper.AddEscapedUserProvidedCode("ab\\\\c", CodeMapper.BackslashEscapeSequenceMatcher);

        // act
        CodeLocation originalLocation = mapper.GetGeneratingCodeLocation(new CodeLocation(1, 5));
        
        // verify
        Assert.Equal(1, originalLocation.Line);
        Assert.Equal(4, originalLocation.Column);
    }
    
    [Theory(DisplayName = $"{nameof(CodeMapper)} returns correct location for multiple escaped codes in sequence")]
    [InlineData("""ab\\\\c""",1,7,1,5)]
    [InlineData("""ab\\\\c""",1,6,1,4)]
    [InlineData("""ab\\\\c""",1,5,1,4)]
    [InlineData("""ab\\\\c""",1,4,1,3)]
    public void CodeMapperReturnsCorrectLocationForMultipleBackSlashEscapedCodesInSequence(string content, int requestedRow, int requestedColumn,
        int expectedRow, int expectedColumn)
    {
        // setup
        CodeMapper mapper = new CodeMapper();
        mapper.AddEscapedUserProvidedCode(content, CodeMapper.BackslashEscapeSequenceMatcher);

        // act
        CodeLocation originalLocation = mapper.GetGeneratingCodeLocation(new CodeLocation(requestedRow, requestedColumn));
        
        // verify
        Assert.Equal(expectedRow, originalLocation.Line);
        Assert.Equal(expectedColumn, originalLocation.Column);
    }

    [Theory(DisplayName = $"{nameof(CodeMapper)} returns correct location for bracket escaped code")]
    [InlineData("ab{{c}}d", 1, 5, 1, 4)]
    [InlineData("ab{{c}}d", 1, 4, 1, 3)]
    [InlineData("ab{{c}}d", 1, 3, 1, 3)]
    public void CodeMapperReturnsCorrectLocationForBracketEscapedCode(string content, int requestedRow, int requestedColumn,
        int expectedRow, int expectedColumn)
    {
        // setup
        CodeMapper mapper = new CodeMapper();
        mapper.AddEscapedUserProvidedCode(content, CodeMapper.BracketEscapeSequenceMatcher);

        // act
        CodeLocation originalLocation = mapper.GetGeneratingCodeLocation(new CodeLocation(requestedRow, requestedColumn));
        
        // verify
        Assert.Equal(expectedRow, originalLocation.Line);
        Assert.Equal(expectedColumn, originalLocation.Column);
    }
}