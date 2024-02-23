using De.Loooping.Templates.Core.Configuration;
using De.Loooping.Templates.Core.ContentReplacers;
using De.Loooping.Templates.Core.TemplateProcessors;
using Moq;

namespace De.Loooping.Templates.Tests.ContentReplacers;

[Trait("TestCategory", "UnitTest")]
public class EnvironmentVariableReplacerTests
{
    [Fact(DisplayName = $"{nameof(EnvironmentVariableReplacer)} replaces placeholders with environment variable content")]
    public void EnvironmentVariableReplacerReplacesPlaceholdersWithEnvironmentVariableContent()
    {
        // setup
        var configMock = new Mock<IContentReplacerConfiguration>();
        IContentReplacer replacer = new EnvironmentVariableReplacer();
        
        Environment.SetEnvironmentVariable("A_TOTALY_SUPERFLUOS_ENVIRONMENT_VARIABLE_FOR_A_TEST", "TestValue");

        string? result = replacer.Replace(configMock.Object, "A_TOTALY_SUPERFLUOS_ENVIRONMENT_VARIABLE_FOR_A_TEST", null);
        Assert.Equal("TestValue", result);
    }

}