using De.Loooping.Templates.Core.CodeMapping;
using De.Loooping.Templates.Core.Template;

namespace De.Loooping.Templates.Core.Tests.Template;

public class LiteralTests
{
    [Theory(DisplayName = "Literals with special characters are correctly returned in the output")]
    [InlineData("start\n\b\t\x0A\x00\n\\end")]
    [InlineData("start\"\n\b\t\x0A\x00\n\\end")]
    [InlineData("start\n\r\u0085\u2028\u2029end")]
    [InlineData("start\U0001F600\u1F60\xE7\x0E7\x00E7end")]
    public void LiteralsWithSpecialCharactersWork(string content)
    {
        // setup
        TemplateBuilder templateBuilder = new TemplateBuilder(content);
        var template = templateBuilder.Build();
        
        // act
        string result = template();
        
        // verify
        Assert.Equal(content, result);
    }

    [Fact(DisplayName = $"{nameof(TemplateBuilder)} and {nameof(CodeMapper)} build correct generating code from literal with special characters")]
    public void TemplateBuilderAndCodeMapperBuildCorrectGeneratingCodeFromLiteralsWithSpecialCharacters()
    {
        // setup
        string content = "start\n\t\b\0\xA01end";
        TemplateBuilder templateBuilder = new TemplateBuilder(content);
        
        // act
        templateBuilder.Build();
        
        // verify
        Assert.Equal(content, templateBuilder.CodeMapper.GeneratingCode);
    }
}