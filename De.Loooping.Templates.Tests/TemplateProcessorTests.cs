using System.Globalization;
using De.Loooping.Templates.Core;
using De.Loooping.Templates.Core.Configuration;
using De.Loooping.Templates.Core.ContentReplacers;
using De.Loooping.Templates.Core.TemplateProcessors;

namespace De.Loooping.Templates.Tests;

[Trait("TestCategory", "UnitTest")]
public class TemplateProcessorTests
{
    #region private classes
    private class SimpleClass
    {
        public string? String { get; set; }
        public DateTime DateTime { get; set; }
        public int Int { get; set; }
        public decimal Decimal { get; set; }
    }
    
    private class NestedClass
    {
        public string? String { get; set; }
        public DateTime DateTime { get; set; }
        public int Int { get; set; }
        public decimal Decimal { get; set; }
        
        public SimpleClass? Child { get; set; }
    }
    #endregion

    private static NestedClass CreateTestObject()
    {
        var testObject = new NestedClass()
        {
            String = "Test",
            DateTime = DateTime.Parse("2023-11-11 00:00:00Z", CultureInfo.InvariantCulture),
            Int = 12345,
            Decimal = 12.345M,
            Child = new SimpleClass()
            {
                String = "TestChild",
                DateTime = DateTime.Parse("2023-11-12 00:00:00Z", CultureInfo.InvariantCulture),
                Int = 123456,
                Decimal = 12.3456M,
            }
        };

        return testObject;
    }

    private static TemplateProcessor GetTemplateProcessorWithObjectPropertyReplacer(object replacementValues)
    {
        TemplateProcessor processor = new TemplateProcessor( new ObjectPropertyReplacer(replacementValues) ,new TemplateProcessorConfiguration());
        return processor;
    }


    [Fact(DisplayName = $"{nameof(TemplateProcessor)} can evaluate first level properties of a data object")]
    public void ProcessorWorkingOnFirstLevelProperties()
    {
        NestedClass testObject = CreateTestObject();
        var processor = GetTemplateProcessorWithObjectPropertyReplacer(testObject);

        string stringResult = processor.Process("-{String}-");
        Assert.Equal("-Test-", stringResult);
        
        string intResult = processor.Process("-{Int}-");
        Assert.Equal("-12345-", intResult);
        
        string decimalResult = processor.Process("-{Decimal}-");
        Assert.Equal("-12.345-", decimalResult);
        
        // This test is unreliable because of daylight saving time
        //string dateResult = replacer.Process("-{DateTime}-");
        //Assert.Equal("-11/11/2023 01:00:00-", dateResult);
        
        string objectResult = processor.Process("-{Child}-");
        Assert.Equal($"-{typeof(SimpleClass).FullName}-", objectResult);
    }

    [Fact(DisplayName = $"{nameof(TemplateProcessor)} can evaluate custom formats")]
    public void ProcessorWorkingWithCustomFormats()
    {
        NestedClass testObject = CreateTestObject();
        var processor = GetTemplateProcessorWithObjectPropertyReplacer(testObject);

        string intResult = processor.Process("-{Int:#,##0}-");
        Assert.Equal("-12,345-", intResult);
        
        string decimalResult = processor.Process("-{Decimal:000.0000}-");
        Assert.Equal("-012.3450-", decimalResult);

        string dateResult = processor.Process("-{DateTime:U}-");
        Assert.Equal("-Saturday, 11 November 2023 00:00:00-", dateResult);
    }
    
    [Fact(DisplayName = $"{nameof(TemplateProcessor)} can evaluate properties on nested levels of a data object")]
    public void ProcessorWorkingOnSecondLevelProperties()
    {
        NestedClass testObject = CreateTestObject();
        var processor = GetTemplateProcessorWithObjectPropertyReplacer(testObject);

        string stringResult = processor.Process("-{Child.String}-");
        Assert.Equal("-TestChild-", stringResult);
        
        string intResult = processor.Process("-{Child.Int}-");
        Assert.Equal("-123456-", intResult);
        
        string decimalResult = processor.Process("-{Child.Decimal}-");
        Assert.Equal("-12.3456-", decimalResult);
        
        string dateResult = processor.Process("-{Child.DateTime:U}-");
        Assert.Equal("-Sunday, 12 November 2023 00:00:00-", dateResult);
    }
    
    [Fact(DisplayName = $"{nameof(TemplateProcessor)} evaluates null properties as an empty string")]
    public void ProcessorReturningEmptyForNull()
    {
        NestedClass testObject = new NestedClass();
        var processor = GetTemplateProcessorWithObjectPropertyReplacer(testObject);

        string objectResult = processor.Process("-{Child}-");
        Assert.Equal("--", objectResult);
    }
    
    [Fact(DisplayName = $"{nameof(TemplateProcessor)} evaluates property references on a null object as an empty string")]
    public void ProcessorReturningEmptyForInvalidNullReferences()
    {
        NestedClass testObject = new NestedClass();
        var processor = GetTemplateProcessorWithObjectPropertyReplacer(testObject);

        string intResult = processor.Process("-{Child.Int}-");
        Assert.Equal("--", intResult);
    }
    
    [Fact(DisplayName = $"{nameof(TemplateProcessor)} does not evaluate formats if {nameof(TemplateProcessorConfiguration)}.{nameof(TemplateProcessorConfiguration.AllowFormatting)} is false")]
    public void ProcessorRejectingFormatsIfAllowFormattingIsFalse()
    {
        NestedClass testObject = CreateTestObject();
        IContentReplacer replacer = new ObjectPropertyReplacer(testObject);
        var processor = new TemplateProcessor(replacer, new TemplateProcessorConfiguration()
        {
            AllowFormatting = false
        });

        string intResult = processor.Process("-{Int:#,##0}-");
        Assert.Equal("-{Int:#,##0}-", intResult); // not a valid placeholder name
    }
    
    [Fact(DisplayName = $"{nameof(TemplateProcessor)} does not evaluate nested levels of a data object if {nameof(TemplateProcessorConfiguration)}.{nameof(TemplateProcessorConfiguration.AllowChildren)} is false")]
    public void ProcessorRejectingSecondLevelIfAllowChildrenIsFalse()
    {
        NestedClass testObject = CreateTestObject();
        IContentReplacer replacer = new ObjectPropertyReplacer(testObject);
        var processor = new TemplateProcessor(replacer, new TemplateProcessorConfiguration()
        {
            AllowChildren = false
        });

        string stringResult = processor.Process("-{Child.String}-");
        Assert.Equal("-{Child.String}-", stringResult); // not a valid placeholder name
    }
    
    [Fact(DisplayName = $"{nameof(TemplateProcessor)} works with custom placeholder delimiters")]
    public void ProcessorEvaluateCustomPlaceholderDelimiters()
    {
        NestedClass testObject = CreateTestObject();
        IContentReplacer replacer = new ObjectPropertyReplacer(testObject);
        var processor = new TemplateProcessor(replacer, new TemplateProcessorConfiguration()
        {
            LeftDelimiter = "[",
            RightDelimiter = "]"
        });

        string stringResult = processor.Process("-[String]-");
        Assert.Equal("-Test-", stringResult);
    }
    
    [Fact(DisplayName = $"{nameof(TemplateProcessor)} works with custom child separator")]
    public void ProcessorEvaluateCustomChildSeparator()
    {
        NestedClass testObject = CreateTestObject();
        IContentReplacer replacer = new ObjectPropertyReplacer(testObject);
        var processor = new TemplateProcessor(replacer, new TemplateProcessorConfiguration()
        {
            ChildSeparator = '#'
        });

        string stringResult = processor.Process("-{Child#String}-");
        Assert.Equal("-TestChild-", stringResult);
    }
    
    [Fact(DisplayName = $"{nameof(TemplateProcessor)} respects custom regular expression for placeholder names")]
    public void ProcessorRespectsCustomPlaceholderNames()
    {
        NestedClass testObject = CreateTestObject();
        IContentReplacer replacer = new ObjectPropertyReplacer(testObject);
        var processor = new TemplateProcessor(replacer, new TemplateProcessorConfiguration()
        {
            PlaceHolderNameRegex = "String|Child"
        });

        string stringResult = processor.Process("-{String}-");
        Assert.Equal("-Test-", stringResult);

        string childStringResult = processor.Process("-{Child.String}-");
        Assert.Equal("-TestChild-", childStringResult);
        
        string intResult = processor.Process("-{Int}-");
        Assert.Equal("-{Int}-", intResult);
    }

    [Fact(DisplayName = $"{nameof(TemplateProcessor)} works with IDictionary")]
    public void ProcessorWorksWithIDictionary()
    {
        System.Collections.IDictionary testObject = new Dictionary<object, object>()
        {
            { "Test", "TestOut" },
            { "Int", 2 }
        };
        var processor = GetTemplateProcessorWithObjectPropertyReplacer(testObject);

        string stringResult = processor.Process("-{Test}-");
        Assert.Equal("-TestOut-", stringResult);

        string intResult = processor.Process("-{Int}-");
        Assert.Equal("-2-", intResult);
    }
    
    private class CustomContentReplacer: IContentReplacer
    {
        public string? Replace(IContentReplacerConfiguration configuration, string placeholderName, string? format)
        {
            return $"[{placeholderName}|{format}]";
        }
    }

    [Theory(DisplayName = $"{nameof(TemplateProcessor)} works with a custom {nameof(IContentReplacer)}")]
    [InlineData("-{test}-", "-[test|]-")]
    [InlineData("-{test.child}-", "-[test.child|]-")]
    [InlineData("-{test:format}-", "-[test|format]-")]
    [InlineData("-{test.child:format}-", "-[test.child|format]-")]
    public void ProcessorWorksWithCustomContentReplacer(string template, string expectedResult)
    {
        // setup
        IContentReplacer replacer = new CustomContentReplacer();
        var processor = new TemplateProcessor(replacer, new TemplateProcessorConfiguration());

        string result = processor.Process(template);
        Assert.Equal(expectedResult, result);
    }

    [Fact(DisplayName = $"{nameof(TemplateProcessor)} works with {nameof(EnvironmentVariableReplacer)}")]
    public void ProcessorWorksWithEnvironmentVariableReplacer()
    {
        // setup
        IContentReplacer replacer = new EnvironmentVariableReplacer();
        var processor = new TemplateProcessor(replacer, new TemplateProcessorConfiguration());
        
        Environment.SetEnvironmentVariable("A_TOTALY_SUPERFLUOS_ENVIRONMENT_VARIABLE_FOR_A_TEST", "TestValue");

        string result = processor.Process("-{A_TOTALY_SUPERFLUOS_ENVIRONMENT_VARIABLE_FOR_A_TEST}-");
        Assert.Equal("-TestValue-", result);
    }
    
}