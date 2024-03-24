using System.Linq.Expressions;
using De.Loooping.Templates.Core.Configuration;
using De.Loooping.Templates.Core.Configuration.Validation;
using De.Loooping.Templates.Core.Tests.Tools;

namespace De.Loooping.Templates.Core.Tests.Configuration;

public class TemplateProcessorConfigurationValidationTests
{
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
}