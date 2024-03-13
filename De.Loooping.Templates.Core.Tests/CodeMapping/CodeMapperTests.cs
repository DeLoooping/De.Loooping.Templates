using De.Loooping.Templates.Core.CodeMapping;
using De.Loooping.Templates.Core.Diagnostic;

namespace De.Loooping.Templates.Core.Tests.CodeMapping;

public class CodeMapperTests
{
    [Fact(DisplayName = $"{nameof(CodeMapper)} returns correct position for one-liners")]
    public void CodeMapperReturnsCorrectPositionForOneLiners()
    {
        // setup
        CodeMapper mapper = new CodeMapper();
        mapper.AddGeneratedCodeFromNil("aa");
        mapper.AddUserProvidedCode("bb");
        mapper.AddGeneratedCodeFromNil("cc");

        // act
        CodeLocation originalLocation = mapper.GetGeneratingCodeLocation(new CodeLocation(0, 2));
        
        // verify
        Assert.Equal(0, originalLocation.Row);
        Assert.Equal(0, originalLocation.Column);
    }

    [Fact(DisplayName = $"{nameof(CodeMapper)} returns correct position for multiline code")]
    public void CodeMapperReturnsCorrectPositionForMultilineCode()
    {
        // setup
        CodeMapper mapper = new CodeMapper();
        mapper.AddGeneratedCodeFromNil("aa\n");
        mapper.AddUserProvidedCode("bb\n");
        mapper.AddGeneratedCodeFromNil("cc");

        // act
        CodeLocation originalLocation = mapper.GetGeneratingCodeLocation(new CodeLocation(1, 1));
        
        // verify
        Assert.Equal(0, originalLocation.Row);
        Assert.Equal(1, originalLocation.Column);
    }
    
    [Fact(DisplayName = $"{nameof(CodeMapper)} returns nearest position to the left of non-mappable internal code")]
    public void CodeMapperReturnsLeftPositionForNonMappableCode()
    {
        // setup
        CodeMapper mapper = new CodeMapper();
        mapper.AddGeneratedCodeFromNil("aa\n");
        mapper.AddUserProvidedCode("bb\n");
        mapper.AddGeneratedCodeFromNil("cc");

        // act
        CodeLocation originalLocation = mapper.GetGeneratingCodeLocation(new CodeLocation(2, 1));
        
        // verify
        Assert.Equal(0, originalLocation.Row);
        Assert.Equal(2, originalLocation.Column);
    }
    
    [Fact(DisplayName = $"{nameof(CodeMapper)} returns correct position and code type for backslash escaped code")]
    public void CodeMapperReturnsCorrectPositionForBackslashEscapedCode()
    {
        // setup
        CodeMapper mapper = new CodeMapper();
        mapper.AddEscapedUserProvidedCode("ab\\\\c", CodeMapper.BackslashEscapeSequenceMatcher);

        // act
        CodeLocation originalLocation = mapper.GetGeneratingCodeLocation(new CodeLocation(0, 4));
        
        // verify
        Assert.Equal(0, originalLocation.Row);
        Assert.Equal(3, originalLocation.Column);
    }
    
    [Theory(DisplayName = $"{nameof(CodeMapper)} returns correct position and code type for multiple escaped codes in sequence")]
    [InlineData("""ab\\\\c""",0,6,0,4)]
    [InlineData("""ab\\\\c""",0,5,0,3)]
    [InlineData("""ab\\\\c""",0,4,0,3)]
    [InlineData("""ab\\\\c""",0,3,0,2)]
    public void CodeMapperReturnsCorrectPositionForMultipleBackSlashEscapedCodesInSequence(string content, int requestedRow, int requestedColumn,
        int expectedRow, int expectedColumn)
    {
        // setup
        CodeMapper mapper = new CodeMapper();
        mapper.AddEscapedUserProvidedCode(content, CodeMapper.BackslashEscapeSequenceMatcher);

        // act
        CodeLocation originalLocation = mapper.GetGeneratingCodeLocation(new CodeLocation(requestedRow, requestedColumn));
        
        // verify
        Assert.Equal(expectedRow, originalLocation.Row);
        Assert.Equal(expectedColumn, originalLocation.Column);
    }

    [Theory(DisplayName = $"{nameof(CodeMapper)} returns correct position and code type for bracket escaped code")]
    [InlineData("ab{{c}}d", 0, 4, 0, 3)]
    [InlineData("ab{{c}}d", 0, 3, 0, 2)]
    [InlineData("ab{{c}}d", 0, 2, 0, 2)]
    public void CodeMapperReturnsCorrectPositionForBracketEscapedCode(string content, int requestedRow, int requestedColumn,
        int expectedRow, int expectedColumn)
    {
        // setup
        CodeMapper mapper = new CodeMapper();
        mapper.AddEscapedUserProvidedCode(content, CodeMapper.BracketEscapeSequenceMatcher);

        // act
        CodeLocation originalLocation = mapper.GetGeneratingCodeLocation(new CodeLocation(requestedRow, requestedColumn));
        
        // verify
        Assert.Equal(expectedRow, originalLocation.Row);
        Assert.Equal(expectedColumn, originalLocation.Column);
    }
}