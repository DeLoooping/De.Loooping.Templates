using De.Loooping.Templates.Core.Configuration;
using De.Loooping.Templates.Core.ContentReplacers;
using De.Loooping.Templates.Core.TemplateProcessors;
using Moq;

namespace De.Loooping.Templates.Tests.TemplateProcessors;

[Trait("TestCategory", "UnitTest")]
public class TemplateProcessorTests
{
    [Fact(DisplayName = $"{nameof(TemplateProcessor)} calls {nameof(IContentReplacer)}.{nameof(IContentReplacer.Replace)} and returns content")]
    public void TemplateProcessorCallsReplaceAndReturnsContent()
    {
        // setup
        var replacerMock = new Mock<IContentReplacer>();
        replacerMock.Setup(r =>
            r.Replace(It.IsAny<IContentReplacerConfiguration>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns("Content");
        
        var templateProcessor = new TemplateProcessor(replacerMock.Object);
        
        // act
        var result = templateProcessor.Process("0{{name0}}1{{ name1 : format1 }}2{{ name2:format2}}3\n4{{name3:format3 }}5");

        // verify
        Assert.Equal("0Content1Content2Content3\n4Content5", result);
        replacerMock.Verify(r=>
            r.Replace(It.IsAny<IContentReplacerConfiguration>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Exactly(4));
        replacerMock.Verify(r=>
            r.Replace(It.IsAny<IContentReplacerConfiguration>(), "name0", null), Times.Once);
        replacerMock.Verify(r=>
            r.Replace(It.IsAny<IContentReplacerConfiguration>(), "name1", " format1 "), Times.Once);
        replacerMock.Verify(r=>
            r.Replace(It.IsAny<IContentReplacerConfiguration>(), "name2", "format2"), Times.Once);
        replacerMock.Verify(r=>
            r.Replace(It.IsAny<IContentReplacerConfiguration>(), "name3", "format3 "), Times.Once);
    }
    
    [Fact(DisplayName = $"{nameof(TemplateProcessor)} processes loops")]
    public void TemplateProcessorProcessesLoops()
    {
        // setup
        var replacerMock = new Mock<IContentReplacer>();
        int callIndex = 0;
        replacerMock.Setup(r =>
            r.Replace(It.IsAny<IContentReplacerConfiguration>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns($"{callIndex++}");
        
        var templateProcessor = new TemplateProcessor(replacerMock.Object);
        
        // act
        var result = templateProcessor.Process(
            "{# for(int i=0;i<3;i++) { #}|{{ somecontent }}|\n{# } #}");

        // verify
        Assert.Equal("|0|\n|1|\n|2|", result);
    }
}