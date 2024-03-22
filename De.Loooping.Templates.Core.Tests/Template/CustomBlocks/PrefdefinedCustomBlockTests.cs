using De.Loooping.Templates.Core.Template.CustomBlocks;

namespace De.Loooping.Templates.Core.Tests.Template.CustomBlocks;

[Trait("TestCategory", "UnitTest")]
public class PrefdefinedCustomBlockTests
{
    private const string _TEST_ENVIRONMENT_VARIABLE_NAME = "COMPLETELY_USELESS_ENVIRONMENT_VARIABLE_FOR_TESTS";

    [Fact(DisplayName = $"{nameof(EnvironmentVariableContentBlock)} works")]
    public void EnvironmentVariableContentBlockWorks()
    {
        // setup
        var customBlock = new EnvironmentVariableContentBlock();
        Environment.SetEnvironmentVariable(_TEST_ENVIRONMENT_VARIABLE_NAME, "42");
        
        // verify
        Assert.Equal("42", customBlock.Evaluate(_TEST_ENVIRONMENT_VARIABLE_NAME));
    }
}