using System.Globalization;
using De.Loooping.Templates.Core;
using De.Loooping.Templates.Core.Configuration;

namespace De.Loooping.Templates.Tests;

[Trait("TestCategory", "UnitTest")]
public class TemplateReplacerTests
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

    private static TemplateReplacer GetDefaultReplacer()
    {
        TemplateReplacer replacer = new TemplateReplacer(new TemplateReplacerConfiguration());
        return replacer;
    }


    [Fact(DisplayName = $"{nameof(TemplateReplacer)} can evaluate first level properties of a data object")]
    public void ReplacerWorkingOnFirstLevel()
    {
        NestedClass testObject = CreateTestObject();
        var replacer = GetDefaultReplacer();

        var formatProvider = CultureInfo.InvariantCulture;

        string stringResult = replacer.ReplacePlaceholders("-{String}-", testObject, formatProvider);
        Assert.Equal("-Test-", stringResult);
        
        string intResult = replacer.ReplacePlaceholders("-{Int}-", testObject, formatProvider);
        Assert.Equal("-12345-", intResult);
        
        string decimalResult = replacer.ReplacePlaceholders("-{Decimal}-", testObject, formatProvider);
        Assert.Equal("-12.345-", decimalResult);
        
        // This test is unreliable because of daylight saving time
        //string dateResult = replacer.Replace("-{DateTime}-", config, formatProvider);
        //Assert.Equal("-11/11/2023 01:00:00-", dateResult);
        
        string objectResult = replacer.ReplacePlaceholders("-{Child}-", testObject, formatProvider);
        Assert.Equal($"-{typeof(SimpleClass).FullName}-", objectResult);
    }

    [Fact(DisplayName = $"{nameof(TemplateReplacer)} can evaluate custom formats")]
    public void ReplacerWorkingWithCustomFormats()
    {
        NestedClass testObject = CreateTestObject();
        var replacer = GetDefaultReplacer();

        var formatProvider = CultureInfo.InvariantCulture;

        string intResult = replacer.ReplacePlaceholders("-{Int:#,##0}-", testObject, formatProvider);
        Assert.Equal("-12,345-", intResult);
        
        string decimalResult = replacer.ReplacePlaceholders("-{Decimal:000.0000}-", testObject, formatProvider);
        Assert.Equal("-012.3450-", decimalResult);

        string dateResult = replacer.ReplacePlaceholders("-{DateTime:U}-", testObject, formatProvider);
        Assert.Equal("-Saturday, 11 November 2023 00:00:00-", dateResult);
    }
    
    [Fact(DisplayName = $"{nameof(TemplateReplacer)} can evaluate properties on nested levels of a data object")]
    public void ReplacerWorkingOnSecondLevel()
    {
        NestedClass testObject = CreateTestObject();
        var replacer = GetDefaultReplacer();

        var formatProvider = CultureInfo.InvariantCulture;

        string stringResult = replacer.ReplacePlaceholders("-{Child.String}-", testObject, formatProvider);
        Assert.Equal("-TestChild-", stringResult);
        
        string intResult = replacer.ReplacePlaceholders("-{Child.Int}-", testObject, formatProvider);
        Assert.Equal("-123456-", intResult);
        
        string decimalResult = replacer.ReplacePlaceholders("-{Child.Decimal}-", testObject, formatProvider);
        Assert.Equal("-12.3456-", decimalResult);
        
        string dateResult = replacer.ReplacePlaceholders("-{Child.DateTime:U}-", testObject, formatProvider);
        Assert.Equal("-Sunday, 12 November 2023 00:00:00-", dateResult);
    }
    
    [Fact(DisplayName = $"{nameof(TemplateReplacer)} evaluates null properties as an empty string")]
    public void ReplacerReturningEmptyForNull()
    {
        NestedClass testObject = new NestedClass();
        var replacer = GetDefaultReplacer();

        var formatProvider = CultureInfo.InvariantCulture;

        string objectResult = replacer.ReplacePlaceholders("-{Child}-", testObject, formatProvider);
        Assert.Equal("--", objectResult);
    }
    
    [Fact(DisplayName = $"{nameof(TemplateReplacer)} evaluates property references on a null object as an empty string")]
    public void ReplacerReturningEmptyForInvalidNullReferences()
    {
        NestedClass testObject = new NestedClass();
        var replacer = GetDefaultReplacer();

        var formatProvider = CultureInfo.InvariantCulture;

        string intResult = replacer.ReplacePlaceholders("-{Child.Int}-", testObject, formatProvider);
        Assert.Equal("--", intResult);
    }
    
    [Fact(DisplayName = $"{nameof(TemplateReplacer)} does not evaluate formats if {nameof(TemplateReplacerConfiguration)}.{nameof(TemplateReplacerConfiguration.AllowFormatting)} is false")]
    public void ReplacerRejectingFormatsIfAllowFormattingIsFalse()
    {
        NestedClass testObject = CreateTestObject();
        var replacer = new TemplateReplacer(new TemplateReplacerConfiguration()
        {
            AllowFormatting = false
        });

        var formatProvider = CultureInfo.InvariantCulture;

        string intResult = replacer.ReplacePlaceholders("-{Int:#,##0}-", testObject, formatProvider);
        Assert.Equal("-{Int:#,##0}-", intResult); // not a valid placeholder name
    }
    
    [Fact(DisplayName = $"{nameof(TemplateReplacer)} does not evaluate nested levels of a data object if {nameof(TemplateReplacerConfiguration)}.{nameof(TemplateReplacerConfiguration.AllowChildren)} is false")]
    public void ReplacerRejectingSecondLevelIfAllowChildrenIsFalse()
    {
        NestedClass testObject = CreateTestObject();
        var replacer = new TemplateReplacer(new TemplateReplacerConfiguration()
        {
            AllowChildren = false
        });

        var formatProvider = CultureInfo.InvariantCulture;

        string stringResult = replacer.ReplacePlaceholders("-{Child.String}-", testObject, formatProvider);
        Assert.Equal("-{Child.String}-", stringResult); // not a valid placeholder name
    }
    
    [Fact(DisplayName = $"{nameof(TemplateReplacer)} works with custom placeholder delimiters")]
    public void ReplacerEvaluateCustomPlaceholderDelimiters()
    {
        NestedClass testObject = CreateTestObject();
        var replacer = new TemplateReplacer(new TemplateReplacerConfiguration()
        {
            LeftDelimiter = "[",
            RightDelimiter = "]"
        });

        var formatProvider = CultureInfo.InvariantCulture;

        string stringResult = replacer.ReplacePlaceholders("-[String]-", testObject, formatProvider);
        Assert.Equal("-Test-", stringResult);
    }
    
    [Fact(DisplayName = $"{nameof(TemplateReplacer)} works with custom child separator")]
    public void ReplacerEvaluateCustomChildSeparator()
    {
        NestedClass testObject = CreateTestObject();
        var replacer = new TemplateReplacer(new TemplateReplacerConfiguration()
        {
            ChildSeparator = '#'
        });

        var formatProvider = CultureInfo.InvariantCulture;

        string stringResult = replacer.ReplacePlaceholders("-{Child#String}-", testObject, formatProvider);
        Assert.Equal("-TestChild-", stringResult);
    }
    
    [Fact(DisplayName = $"{nameof(TemplateReplacer)} respects custom regular expression for placeholder names")]
    public void ReplacerRespectsCustomPlaceholderNames()
    {
        NestedClass testObject = CreateTestObject();
        var replacer = new TemplateReplacer(new TemplateReplacerConfiguration()
        {
            PlaceHolderNameRegex = "String|Child"
        });

        var formatProvider = CultureInfo.InvariantCulture;

        string stringResult = replacer.ReplacePlaceholders("-{String}-", testObject, formatProvider);
        Assert.Equal("-Test-", stringResult);

        string childStringResult = replacer.ReplacePlaceholders("-{Child.String}-", testObject, formatProvider);
        Assert.Equal("-TestChild-", childStringResult);
        
        string intResult = replacer.ReplacePlaceholders("-{Int}-", testObject, formatProvider);
        Assert.Equal("-{Int}-", intResult);
    }

    [Fact(DisplayName = $"{nameof(TemplateReplacer)} works with IDictionary")]
    public void ReplacerWorksWithIDictionary()
    {
        System.Collections.IDictionary testObject = new Dictionary<object, object>()
        {
            { "Test", "TestOut" },
            { "Int", 2 }
        };
        var replacer = GetDefaultReplacer();

        var formatProvider = CultureInfo.InvariantCulture;

        string stringResult = replacer.ReplacePlaceholders("-{Test}-", testObject, formatProvider);
        Assert.Equal("-TestOut-", stringResult);

        string intResult = replacer.ReplacePlaceholders("-{Int}-", testObject, formatProvider);
        Assert.Equal("-2-", intResult);
    }

    [Theory(DisplayName = $"{nameof(TemplateReplacer)} works with a {nameof(TemplateReplacer.PlaceholderReplacer)} delegate")]
    [InlineData("-{test}-", "-[test|]-")]
    [InlineData("-{test.child}-", "-[test.child|]-")]
    [InlineData("-{test:format}-", "-[test|format]-")]
    [InlineData("-{test.child:format}-", "-[test.child|format]-")]
    public void ReplacerWorksWithPlaceholderReplacerDelegate(string template, string expectedResult)
    {
        // setup
        TemplateReplacer.PlaceholderReplacer replacerDelegate = (key, format) =>
        {
            return $"[{key}|{format}]";
        };

        var replacer = GetDefaultReplacer();

        string result = replacer.ReplacePlaceholders(template, replacerDelegate);
        Assert.Equal(expectedResult, result);
    }
    
}