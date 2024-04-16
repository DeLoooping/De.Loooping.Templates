using De.Loooping.Templates.Core.Template.CustomBlocks;

namespace De.Loooping.Templates.Core.Tests.Template.CustomBlocks;

[Trait("TestCategory", "UnitTest")]
public class EnvironmentVariableContentBlockTests
{
    private const string _TEST_ENVIRONMENT_VARIABLE_NAME = "COMPLETELY_USELESS_ENVIRONMENT_VARIABLE_FOR_TESTS";

    [Fact(DisplayName = $"{nameof(EnvironmentVariableContentBlock)} returns environment variable content")]
    public void EnvironmentVariableContentBlockReturnsContent()
    {
        // setup
        var customBlock = new EnvironmentVariableContentBlock();
        Environment.SetEnvironmentVariable(_TEST_ENVIRONMENT_VARIABLE_NAME, "42");
        
        // verify
        Assert.Equal("42", customBlock.Evaluate(_TEST_ENVIRONMENT_VARIABLE_NAME));
    }

    [Fact(DisplayName = $"{nameof(EnvironmentVariableContentBlock)} returns default value if environment variable is not set")]
    public void EnvironmentVariableContentBlockReturnsDefaultValue()
    {
        // setup
        var customBlock = new EnvironmentVariableContentBlock(":");
        
        // verify
        Assert.Equal("21", customBlock.Evaluate($"{_TEST_ENVIRONMENT_VARIABLE_NAME}:21"));
    }

    [Fact(DisplayName = $"{nameof(EnvironmentVariableContentBlock)} returns default value if environment variable is set to empty string")]
    public void EnvironmentVariableContentBlockReturnsDefaultValueWhenVariableIsEmpty()
    {
        // setup
        var customBlock = new EnvironmentVariableContentBlock(":");
        Environment.SetEnvironmentVariable(_TEST_ENVIRONMENT_VARIABLE_NAME, "");

        // verify
        Assert.Equal("21", customBlock.Evaluate($"{_TEST_ENVIRONMENT_VARIABLE_NAME}:21"));
    }
}