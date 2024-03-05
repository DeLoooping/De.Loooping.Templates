using De.Loooping.Templates.Core.Configuration;
using De.Loooping.Templates.Core.Template;
using De.Loooping.Templates.Core.TemplateProcessors;

namespace De.Loooping.Templates.Tests.Template;

[Trait("TestCategory", "UnitTest")]
public class TemplateTests
{
    public class Counter
    {
        private int _i = 0;

        public int CountUp()
        {
            return _i++;
        }
    }

    private delegate string CounterDelegate(Counter counter);

    [Fact(DisplayName = "Template processes loops")]
    public void TemplateProcessesLoops()
    {
        // setup
        var counter = new Counter();
        string content = "{% for(int i=0;i<3;i++) { %}|{{ counter.CountUp().ToString() }}|\n{% } %}";

        var templateProcessor = new TemplateBuilder<CounterDelegate>(content)
            .WithType<object>()
            .WithType<Counter>();
        
        // act
        var template = templateProcessor.Build();
        var result = template(counter);

        // verify
        Assert.Equal("|0|\n|1|\n|2|\n", result);
    }

    [Fact(DisplayName = "Template processes local variables")]
    public void TemplateProcessesLocalVariables()
    {
        // setup
        string content = "{% for(int i=0; i<3; i++) { %}{{ i.ToString() }}{% } %}";
        TemplateBuilder templateBuilder = new TemplateBuilder(content)
            .WithType<int>();
        var template = templateBuilder.Build();
        
        // act
        string result = template();
        
        // verify
        Assert.Equal("012", result);
    }

    [Fact(DisplayName = "Template converts integers to string")]
    public void TemplateConvertsIntegers()
    {
        // setup
        string content = "{{ 42 }}";
        TemplateBuilder templateBuilder = new TemplateBuilder(content)
            .WithType<int>();
        var template = templateBuilder.Build();
        
        // act
        string result = template();
        
        // verify
        Assert.Equal("42", result);
    }

    [Fact(DisplayName = "Content can be formatted")]
    public void ContentCanBeFormatted()
    {
        // setup
        string content = "{{ 42.42 :000.000}}";
        TemplateBuilder templateBuilder = new TemplateBuilder(content)
            .WithType<int>();
        var template = templateBuilder.Build();
        
        // act
        string result = template();
        
        // verify
        Assert.Equal("042.420", result);
    }

    [Fact(DisplayName = "Content with curly braces can be formatted")]
    public void ContentWithCurlyBracesCanBeFormatted()
    {
        // setup
        string content = "{{ \"{{}}\".Length :000.000}}";
        TemplateBuilder templateBuilder = new TemplateBuilder(content)
            .WithType<int>();
        var template = templateBuilder.Build();
        
        // act
        string result = template();
        
        // verify
        Assert.Equal("004.000", result);
    }

    [Theory(DisplayName = "Content stringifies expressions")]
    [InlineData("{{ 2*21 }}", "42")]
    [InlineData("{{ \"abc\" }}", "abc")]
    [InlineData("{{ 12.34 }}", "12.34")]
    [InlineData("{{ typeof(int) }}", "System.Int32")]
    public void ContentStringifiesExpressions(string content, string expectedResult)
    {
        TemplateBuilder templateBuilder = new TemplateBuilder(content);
        var template = templateBuilder.Build();
        
        // act
        string result = template();
        
        // verify
        Assert.Equal(expectedResult, result);
    }

    [Theory(DisplayName = "Non-expression content throws")]
    [InlineData("{{ var x = 1 }}")]
    [InlineData("{{ yield return \"a\" }}")]
    [InlineData("""{{ "abc"; }}""")]
    public void NonExpressionContentThrows(string content)
    {
        // setup
        TemplateBuilder templateBuilder = new TemplateBuilder(content)
            .WithType<int>();
        
        // act and verify
        Assert.Throws<SyntaxErrorException>(() => templateBuilder.Build());
    }
}