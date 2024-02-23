using System.Globalization;
using De.Loooping.Templates.Core.Configuration;
using De.Loooping.Templates.Core.ContentReplacers;
using Moq;

namespace De.Loooping.Templates.Tests.ContentReplacers;

[Trait("TestCategory", "UnitTest")]
public class ObjectPropertyReplacerTests
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
    
    [Theory(DisplayName = $"{nameof(ObjectPropertyReplacer)} can evaluate first level properties of a data object")]
    [InlineData("String", "Test")]
    [InlineData("Int", "12345")]
    [InlineData("Decimal", "12.345")]
    //[InlineData("DateTime", "11/11/2023 01:00:00")] // This test is unreliable because of daylight saving time
    [InlineData("Child", "De.Loooping.Templates.Tests.ContentReplacers.ObjectPropertyReplacerTests+SimpleClass")] // typeof(SimpleClass).FullName
    public void ObjectPropertyReplacerWorkingOnFirstLevelProperties(string placeholderName, string? expectedResult)
    {
        var configMock = new Mock<IContentReplacerConfiguration>();
        NestedClass testObject = CreateTestObject();
        IContentReplacer replacer = new ObjectPropertyReplacer(testObject);

        string? stringResult = replacer.Replace(configMock.Object, placeholderName, null);
        Assert.Equal(expectedResult, stringResult);
    }
    
    [Theory(DisplayName = $"{nameof(ObjectPropertyReplacer)} can evaluate custom formats")]
    [InlineData("Int", "#,##0", "12,345")]
    [InlineData("Decimal", "000.0000", "012.3450")]
    [InlineData("DateTime", "U", "Saturday, 11 November 2023 00:00:00")]
    public void ObjectPropertyReplacerWorkingWithCustomFormats(string placeholderName, string format, string? expectedResult)
    {
        var configMock = new Mock<IContentReplacerConfiguration>();
        configMock.Setup(c => c.AllowChildren).Returns(true);
        configMock.Setup(c => c.ChildSeparator).Returns('.');
        configMock.Setup(c => c.AllowFormatting).Returns(true);

        NestedClass testObject = CreateTestObject();
        IContentReplacer replacer = new ObjectPropertyReplacer(testObject);

        string? stringResult = replacer.Replace(configMock.Object, placeholderName, format);
        Assert.Equal(expectedResult, stringResult);
    }
    
    [Theory(DisplayName = $"{nameof(ObjectPropertyReplacer)} can evaluate properties on nested levels of a data object")]
    [InlineData("Child.String", null, "TestChild")]
    [InlineData("Child.Int", null, "123456")]
    [InlineData("Child.Decimal", null, "12.3456")]
    [InlineData("Child.DateTime", "U", "Sunday, 12 November 2023 00:00:00")]
    public void ObjectPropertyReplacerWorkingOnSecondLevelProperties(string placeholderName, string? format, string? expectedResult)
    {
        var configMock = new Mock<IContentReplacerConfiguration>();
        configMock.Setup(c => c.AllowChildren).Returns(true);
        configMock.Setup(c => c.ChildSeparator).Returns('.');
        configMock.Setup(c => c.AllowFormatting).Returns(true);

        NestedClass testObject = CreateTestObject();
        IContentReplacer replacer = new ObjectPropertyReplacer(testObject);

        string? stringResult = replacer.Replace(configMock.Object, placeholderName, format);
        Assert.Equal(expectedResult, stringResult);
    }
    
    [Fact(DisplayName = $"{nameof(ObjectPropertyReplacer)} does not evaluate formats if {nameof(IContentReplacerConfiguration)}.{nameof(IContentReplacerConfiguration.AllowFormatting)} is false")]
    public void ObjectPropertyReplacerRejectingFormatsIfAllowFormattingIsFalse()
    {
        var configMock = new Mock<IContentReplacerConfiguration>();
        configMock.Setup(c => c.AllowFormatting).Returns(false);

        NestedClass testObject = CreateTestObject();
        IContentReplacer replacer = new ObjectPropertyReplacer(testObject);

        string? intResult = replacer.Replace(configMock.Object, "Int","#,##0");
        Assert.Equal("12345", intResult); // not a valid placeholder name
    }
    
    [Fact(DisplayName = $"{nameof(ObjectPropertyReplacer)} does not evaluate nested levels of a data object if {nameof(IContentReplacerConfiguration)}.{nameof(IContentReplacerConfiguration.AllowChildren)} is false")]
    public void ObjectPropertyReplacerRejectingSecondLevelIfAllowChildrenIsFalse()
    {
        var configMock = new Mock<IContentReplacerConfiguration>();
        configMock.Setup(c => c.AllowChildren).Returns(false);

        NestedClass testObject = CreateTestObject();
        IContentReplacer replacer = new ObjectPropertyReplacer(testObject);

        string? stringResult = replacer.Replace(configMock.Object, "Child.String", null);
        Assert.Null(stringResult);
    }
    
    [Fact(DisplayName = $"{nameof(ObjectPropertyReplacer)} works with custom child separator")]
    public void ObjectPropertyReplacerWorksWithCustomChildSeparator()
    {
        var configMock = new Mock<IContentReplacerConfiguration>();
        configMock.Setup(c => c.AllowChildren).Returns(true);
        configMock.Setup(c => c.ChildSeparator).Returns('#');
        
        NestedClass testObject = CreateTestObject();
        IContentReplacer replacer = new ObjectPropertyReplacer(testObject);

        string? stringResult = replacer.Replace(configMock.Object, "Child#String", null);
        Assert.Equal("TestChild", stringResult);
    }
    
    [Fact(DisplayName = $"{nameof(ObjectPropertyReplacer)} works with IDictionary")]
    public void ObjectPropertyReplacerWorksWithIDictionary()
    {
        var configMock = new Mock<IContentReplacerConfiguration>();
        System.Collections.IDictionary testObject = new Dictionary<object, object>()
        {
            { "Test", "TestOut" },
            { "Int", 2 }
        };
        var replacer = new ObjectPropertyReplacer(testObject);

        string? stringResult = replacer.Replace(configMock.Object, "Test", null);
        Assert.Equal("TestOut", stringResult);

        string? intResult = replacer.Replace(configMock.Object, "Int", null);
        Assert.Equal("2", intResult);
    }
}