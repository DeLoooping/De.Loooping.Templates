using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace De.Loooping.Templates.Configuration.Tests;

public class JsonTemplateConfigurationExtensionsTests
{
    class TestConfigurationSection
    {
        public int A { get; set; }
        public List<int> B { get; set; }
        public string C { get; set; }
        public List<string> D { get; set; }
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
        Assert.Equal([1,2,3], config.B);
        Assert.Equal("lala", config.C);
        Assert.Equal(["000", "001", "002"], config.D);
    }

    public class TestData
    {
        public int Int { get; set; }
        public List<int> IntList { get; set; }
        public string String { get; set; }
        public List<string> StringList { get; set; }
    }

    delegate string TestDataDelegate(TestData input);
    
    [Fact(DisplayName = "Json template configuration with injected data returns expected values")]
    public void JsonTemplateConfigurationWithInjectedDataWorks()
    {
        TestData testData = new TestData()
        {
            Int = 43,
            IntList = [1, 2, 3, 4],
            String = "lalala",
            StringList = ["a", "b", "c", "d"]
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