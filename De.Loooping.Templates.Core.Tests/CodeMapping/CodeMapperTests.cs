using System.Text.RegularExpressions;
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
        CodePosition originalPosition = mapper.GetGeneratingCodePosition(new CodePosition(0, 2));
        
        // verify
        Assert.Equal(0, originalPosition.Row);
        Assert.Equal(0, originalPosition.Column);
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
        CodePosition originalPosition = mapper.GetGeneratingCodePosition(new CodePosition(1, 1));
        
        // verify
        Assert.Equal(0, originalPosition.Row);
        Assert.Equal(1, originalPosition.Column);
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
        CodePosition originalPosition = mapper.GetGeneratingCodePosition(new CodePosition(2, 1));
        
        // verify
        Assert.Equal(0, originalPosition.Row);
        Assert.Equal(2, originalPosition.Column);
    }
    
    [Fact(DisplayName = $"{nameof(CodeMapper)} returns correct position and code type for backslash escaped code")]
    public void CodeMapperReturnsCorrectPositionForBackslashEscapedCode()
    {
        // setup
        CodeMapper mapper = new CodeMapper();
        Regex escapeRegex = new Regex(@"\G(?<escape>\\)(?<escaped>.)");
        mapper.AddEscapedUserProvidedCode("ab\\\\c", escapeRegex);

        // act
        CodePosition originalPosition = mapper.GetGeneratingCodePosition(new CodePosition(0, 4));
        
        // verify
        Assert.Equal(0, originalPosition.Row);
        Assert.Equal(3, originalPosition.Column);
    }
    
    [Fact(DisplayName = $"{nameof(CodeMapper)} returns correct position and code type for multiple escaped codes in sequence")]
    public void CodeMapperReturnsCorrectPositionForMultipleBackSlashEscapedCodesInSequence()
    {
        // setup
        CodeMapper mapper = new CodeMapper();
        Regex escapeRegex = new Regex(@"\G(?<escape>\\)(?<escaped>.)");
        mapper.AddEscapedUserProvidedCode("""ab\\\\c""", escapeRegex);

        // act
        CodePosition originalPosition1 = mapper.GetGeneratingCodePosition(new CodePosition(0, 6));
        CodePosition originalPosition2 = mapper.GetGeneratingCodePosition(new CodePosition(0, 5));
        CodePosition originalPosition3 = mapper.GetGeneratingCodePosition(new CodePosition(0, 4));
        CodePosition originalPosition4 = mapper.GetGeneratingCodePosition(new CodePosition(0, 3));
        
        // verify
        Assert.Equal(0, originalPosition1.Row);
        Assert.Equal(4, originalPosition1.Column);
        
        Assert.Equal(0, originalPosition2.Row);
        Assert.Equal(3, originalPosition2.Column);
        
        Assert.Equal(0, originalPosition3.Row);
        Assert.Equal(3, originalPosition3.Column);
        
        Assert.Equal(0, originalPosition4.Row);
        Assert.Equal(2, originalPosition4.Column);
    }

    [Fact(DisplayName = $"{nameof(CodeMapper)} returns correct position and code type for bracket escaped code")]
    public void CodeMapperReturnsCorrectPositionForBracketEscapedCode()
    {
        // setup
        CodeMapper mapper = new CodeMapper();
        Regex escapeRegex = new Regex(@"\G((?<escape>\{)(?<escaped>\{)|(?<escape>\})(?<escaped>\}))");
        mapper.AddEscapedUserProvidedCode("ab{{c}}d", escapeRegex);

        // act
        CodePosition originalPosition1 = mapper.GetGeneratingCodePosition(new CodePosition(0, 4));
        CodePosition originalPosition2 = mapper.GetGeneratingCodePosition(new CodePosition(0, 3));
        CodePosition originalPosition3 = mapper.GetGeneratingCodePosition(new CodePosition(0, 2));
        
        // verify
        Assert.Equal(0, originalPosition1.Row);
        Assert.Equal(3, originalPosition1.Column);
        
        Assert.Equal(0, originalPosition2.Row);
        Assert.Equal(2, originalPosition2.Column);
        
        Assert.Equal(0, originalPosition3.Row);
        Assert.Equal(2, originalPosition3.Column);
    }
}