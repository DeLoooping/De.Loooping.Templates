using De.Loooping.Templates.Core.Template.CustomBlocks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace De.Loooping.Templates.Configuration.Tests;

public class JsonTemplateConfigurationExtensionsTests
{
    class TestConfigurationSection
    {
        public required int A { get; set; }
        public required List<int> B { get; set; }
        public required string C { get; set; }
        public required List<string> D { get; set; }
    }
    
    [Fact(DisplayName = "Basic json template configuration returns expected values")]
    public void BasicJsonTemplateConfigurationWorks()
    {
        ConfigurationManager configuration = new ConfigurationManager();
        configuration.AddJsonTemplateFile("Resources/BasicJsonTemplateConfiguration.json");

        var serviceProvider = new ServiceCollection()
            .Configure<TestConfigurationSection>(configuration.GetSection("TestSection"))
            .BuildServiceProvider();

        var config = serviceProvider.GetRequiredService<IOptions<TestConfigurationSection>>().Value;
        
        Assert.Equal(42, config.A);
        Assert.Equal(new List<int> { 1, 2, 3 }, config.B);
        Assert.Equal("lala", config.C);
        Assert.Equal(new List<string> { "000", "001", "002" }, config.D);
    }

    public class TestData
    {
        public required int Int { get; set; }
        public required List<int> IntList { get; set; }
        public required string String { get; set; }
        public required List<string> StringList { get; set; }
    }

    delegate string TestDataDelegate(TestData input);
    
    [Fact(DisplayName = "Json template configuration with injected data returns expected values")]
    public void JsonTemplateConfigurationWithInjectedDataWorks()
    {
        TestData testData = new TestData()
        {
            Int = 43,
            IntList = new List<int> { 1, 2, 3, 4 },
            String = "lalala",
            StringList = new List<string> { "a", "b", "c", "d" }
        };
        
        ConfigurationManager configuration = new ConfigurationManager();
        configuration.AddJsonTemplateFile<TestDataDelegate>("Resources/JsonTemplateConfigurationWithInjectedData.json",
            (d) => d(testData),
            build: builder =>
            {
                builder.AddType(typeof(List<>));
            });

        var serviceProvider = new ServiceCollection()
            .Configure<TestConfigurationSection>(configuration.GetSection("TestSection"))
            .BuildServiceProvider();

        var config = serviceProvider.GetRequiredService<IOptions<TestConfigurationSection>>().Value;
        
        Assert.Equal(testData.Int, config.A);
        Assert.Equal(testData.IntList, config.B);
        Assert.Equal(testData.String, config.C);
        Assert.Equal(testData.StringList, config.D);
    }
}