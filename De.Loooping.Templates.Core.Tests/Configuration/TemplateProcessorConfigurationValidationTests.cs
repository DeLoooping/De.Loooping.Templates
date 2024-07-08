using De.Loooping.Templates.Core.Configuration;
using De.Loooping.Templates.Core.Configuration.Validation;
using De.Loooping.Templates.Core.Template;
using De.Loooping.Templates.Core.Template.CustomBlocks;
using De.Loooping.Templates.Core.Tests.Tools;
using Xunit.Abstractions;

namespace De.Loooping.Templates.Core.Tests.Configuration;

public class TemplateProcessorConfigurationValidationTests
{
    private readonly ITestOutputHelper _outputHelper;

    public TemplateProcessorConfigurationValidationTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }
    
    [Fact(DisplayName = "Default configuration passes")]
    public void DefaultConfigurationPasses()
    {
        // setup
        TemplateProcessorConfigurationValidation validation = new TemplateProcessorConfigurationValidation();
        TemplateProcessorConfiguration configuration = new TemplateProcessorConfiguration();

        // act
        var result = validation.Validate(null, configuration);
        
        // verify
        Assert.True(result.Succeeded);
        Assert.False(result.Failed);
    }

    public delegate void SetDelimiter(TemplateProcessorConfiguration config, string value);

    public class DelimiterSetter
    {
        private readonly SetDelimiter _setDelimiter;
        private readonly string _name;

        public DelimiterSetter(SetDelimiter setDelimiter, string name)
        {
            _setDelimiter = setDelimiter;
            _name = name;
        }
        
        public void SetDelimiter(TemplateProcessorConfiguration configuration, string value)
        {
            _setDelimiter(configuration, value);
        }

        public override string ToString()
        {
            return $"{_name}";
        }
    }

    private class LeftDelimiterSetterData : TestData<(DelimiterSetter firstDelimiterSetter, DelimiterSetter secondDelimiterSetter)>
    {
        protected override IEnumerable<(DelimiterSetter firstDelimiterSetter, DelimiterSetter secondDelimiterSetter)> GetData()
        {
            DelimiterSetter[] leftDelimiterSetters =
                {
                    new((config, value) => config.LeftContentDelimiter = value, nameof(TemplateProcessorConfiguration.LeftContentDelimiter)),
                    new((config, value) => config.LeftStatementDelimiter = value, nameof(TemplateProcessorConfiguration.LeftStatementDelimiter)),
                    new((config, value) => config.LeftCommentDelimiter = value, nameof(TemplateProcessorConfiguration.LeftCommentDelimiter)),
                    new((config, value) => config.LeftCustomBlockDelimiter = value, nameof(TemplateProcessorConfiguration.LeftCustomBlockDelimiter)),
                };

            foreach (var firstSetter in leftDelimiterSetters)
            {
                foreach (var secondSetter in leftDelimiterSetters)
                {
                    if (firstSetter != secondSetter)
                    {
                        yield return (firstSetter, secondSetter);
                    }
                }
            }
        }
    }
    
    [Theory(DisplayName = "Same left delimiters fail")]
    [ClassData(typeof(LeftDelimiterSetterData))]
    public void SameLeftDelimitersFail(DelimiterSetter firstDelimiterSetter, DelimiterSetter secondDelimiterSetter)
    {
        // setup
        TemplateProcessorConfigurationValidation validation = new TemplateProcessorConfigurationValidation();
        TemplateProcessorConfiguration configuration = new TemplateProcessorConfiguration();
        firstDelimiterSetter.SetDelimiter(configuration, "{{{§");
        secondDelimiterSetter.SetDelimiter(configuration, "{{{§");

        // act
        var result = validation.Validate(null, configuration);
        
        // verify
        Assert.False(result.Succeeded);
        Assert.True(result.Failed);
    }
    
    [Theory(DisplayName = "Left delimiters with identical start fail")]
    [ClassData(typeof(LeftDelimiterSetterData))]
    public void LeftDelimitersWithIdenticalStartFail(DelimiterSetter firstDelimiterSetter, DelimiterSetter secondDelimiterSetter)
    {
        // setup
        TemplateProcessorConfigurationValidation validation = new TemplateProcessorConfigurationValidation();
        TemplateProcessorConfiguration configuration = new TemplateProcessorConfiguration();
        firstDelimiterSetter.SetDelimiter(configuration, "{{{§");
        secondDelimiterSetter.SetDelimiter(configuration, "{{{");

        // act
        var result = validation.Validate(null, configuration);
        
        // verify
        Assert.False(result.Succeeded);
        Assert.True(result.Failed);
    }

    class BlockDeactivationTestData : TestData<(string name, Action<TemplateProcessorConfiguration> adjustConfig, Action<TemplateProcessorConfigurationValidation>? adjustValidator, bool isValid)>
    {
        protected override IEnumerable<(string name, Action<TemplateProcessorConfiguration> adjustConfig, Action<TemplateProcessorConfigurationValidation>? adjustValidator, bool isValid)> GetData()
        {
            yield return ("Content blocks deactivated", (c) =>
            {
                c.EvaluateContentBlocks = false;
                c.LeftContentDelimiter = "{%";
                c.LeftStatementDelimiter = "{%";
            }, null, true);
            yield return ("Content blocks not deactivated", (c) =>
            {
                c.EvaluateContentBlocks = true;
                c.LeftContentDelimiter = "{%";
                c.LeftStatementDelimiter = "{%";
            }, null, false);
            yield return ("Statement blocks deactivated", (c) =>
            {
                c.EvaluateStatementBlocks = false;
                c.LeftContentDelimiter = "{{";
                c.LeftStatementDelimiter = "{{";
            }, null, true);
            yield return ("Statement blocks not deactivated", (c) =>
            {
                c.EvaluateStatementBlocks = true;
                c.LeftContentDelimiter = "{{";
                c.LeftStatementDelimiter = "{{";
            }, null, false);
            yield return ("Comment blocks deactivated", (c) =>
            {
                c.RemoveCommentBlocks = false;
                c.LeftContentDelimiter = "{#";
                c.LeftCommentDelimiter = "{#";
            }, null, true);
            yield return ("Comment blocks not deactivated", (c) =>
            {
                c.RemoveCommentBlocks = true;
                c.LeftContentDelimiter = "{#";
                c.LeftCommentDelimiter = "{#";
            }, null, false);
            yield return ("All non-custom blocks deactivated", (c) =>
            {
                c.EvaluateContentBlocks = false;
                c.EvaluateStatementBlocks = false;
                c.RemoveCommentBlocks = false;
                c.LeftContentDelimiter = "{{";
                c.LeftStatementDelimiter = "{{";
                c.LeftCommentDelimiter = "{{";
            }, null, true);
            yield return ("With customblocks", (c) =>
            {
                c.LeftContentDelimiter = "{$";
                c.LeftCustomBlockDelimiter = "{$";
            }, v => v.CheckCustomBlockConfiguration = true,
                false);
            yield return ("Without customblocks", (c) =>
            {
                c.LeftContentDelimiter = "{$";
                c.LeftCustomBlockDelimiter = "{$";
            }, v => v.CheckCustomBlockConfiguration = false,
                true);
        }
    }

    [Theory(DisplayName = "Left delimiters of deactivated blocks are not respected during configuration validation")]
    [ClassData(typeof(BlockDeactivationTestData))]
    public void LeftDelimiterOfDeactivatedBlocksAreNotRespectedDuringConfigurationValidation(
        string name,
        Action<TemplateProcessorConfiguration> adjustConfig,
        Action<TemplateProcessorConfigurationValidation>? adjustValidator,
        bool isValid)
    {
        // setup
        TemplateProcessorConfigurationValidation validation = new TemplateProcessorConfigurationValidation();
        if (adjustValidator != null)
            adjustValidator(validation);
        
        TemplateProcessorConfiguration configuration = new TemplateProcessorConfiguration();
        adjustConfig(configuration);

        // act
        var result = validation.Validate(null, configuration);
        
        // verify
        if (result.Failed || !result.Succeeded)
        {
            _outputHelper.WriteLine(String.Join("\n", result.Failures ?? Enumerable.Empty<string>()));
        }
        Assert.Equal(isValid, result.Succeeded);
        Assert.Equal(!isValid, result.Failed);
    }
    
    [Fact(DisplayName = $"{nameof(TemplateBuilder)} throws if configuration is not valid")]
    public void TemplateBuilderThrowsOnBuildIfConfigurationIsNotValid()
    {
        TemplateBuilder builder = new TemplateBuilder("nothing");
        builder.Configuration.LeftContentDelimiter = "{";
        builder.Configuration.LeftStatementDelimiter = "{%";

        Assert.Throws<ArgumentException>(() => builder.Build());
    }
    
    private class TestCustomBlock : ICustomBlock
    {
        public string DefaultIdentifier => "Test";
        public string Evaluate(string content)
        {
            return "Test";
        }
    }
    
    [Fact(DisplayName = $"{nameof(TemplateBuilder)} throws if custom block configuration is not valid and there are custom blocks")]
    public void TemplateBuilderThrowsOnBuildIfCustomBlockConfigurationIsNotValidAndThereAreCustomBlocks()
    {
        TemplateBuilder builder = new TemplateBuilder("nothing")
            .WithCustomBlock(new TestCustomBlock());
        builder.Configuration.LeftContentDelimiter = "{$";
        builder.Configuration.LeftCustomBlockDelimiter = "{$";

        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Fact(DisplayName = $"{nameof(TemplateBuilder)} throws not if custom block configuration is not valid, but there are no custom blocks")]
    public void TemplateBuilderThrowsNotOnBuildIfCustomBlockConfigurationIsNotValidButThereAreNoCustomBlocks()
    {
        TemplateBuilder builder = new TemplateBuilder("nothing");
        builder.Configuration.LeftContentDelimiter = "{$";
        builder.Configuration.LeftCustomBlockDelimiter = "{$";

        builder.Build();
    }
}