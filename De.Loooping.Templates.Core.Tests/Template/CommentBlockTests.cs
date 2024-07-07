using De.Loooping.Templates.Core.Configuration;
using De.Loooping.Templates.Core.Template;

namespace De.Loooping.Templates.Core.Tests.Template;

[Trait("TestCategory", "UnitTest")]
public class CommentBlockTests
{
    [Fact(DisplayName = $"Comment blocks are removed")]
    public void CommentBlocksAreRemoved()
    {
        // setup
        string content = "a{# test comment #}a";
        TemplateBuilder templateBuilder = new TemplateBuilder(content)
            .WithType<int>();
        templateBuilder.Configuration.EvaluateContentBlocks = false;
        var template = templateBuilder.Build();

        // act
        string result = template();

        // verify
        Assert.Equal("aa", result);
    }

    [Fact(DisplayName = $"Comment block is not removed when {nameof(TemplateProcessorConfiguration)}.{nameof(TemplateProcessorConfiguration.RemoveCommentBlocks)} = false")]
    public void CommentBlockIsNotRemovedWhenRemoveCommentBlocksIsFalse()
    {
        // setup
        string content = "a{# test comment #}a";
        TemplateBuilder templateBuilder = new TemplateBuilder(content)
            .WithType<int>();
        templateBuilder.Configuration.RemoveCommentBlocks = false;
        var template = templateBuilder.Build();

        // act
        string result = template();

        // verify
        Assert.Equal(content, result);
    }

}