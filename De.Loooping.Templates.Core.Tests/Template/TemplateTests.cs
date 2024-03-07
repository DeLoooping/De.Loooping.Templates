using De.Loooping.Templates.Core.Template;
using De.Loooping.Templates.Core.TemplateProcessors;
using Xunit.Abstractions;

namespace De.Loooping.Templates.Core.Tests.Template;

[Trait("TestCategory", "UnitTest")]
public class TemplateTests
{
    private readonly ITestOutputHelper _outputHelper;

    public TemplateTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }
    
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

        var templateBuilder = new TemplateBuilder<CounterDelegate>(content)
            .WithType<object>()
            .WithType<Counter>();
        
        // act
        var template = templateBuilder.Build();
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

    [Fact(DisplayName = "Multiline content works")]
    public void MultilineContentWorks()
    {
        string content = "{{ \"abc\"\n.ToUpper()\n+\"DEF\"\n.ToLower() }}";
        TemplateBuilder templateBuilder = new TemplateBuilder(content);
        var template = templateBuilder.Build();
        
        // act
        string result = template();
        
        // verify
        Assert.Equal("ABCdef", result);
    }

    [Fact(DisplayName = "Content with newline and spaces at end works")]
    public void ContentWithNewlineAndSpacesAtEndWorks()
    {
        string content = "{{ \"abc\"\n.ToUpper()\n  }}";
        TemplateBuilder templateBuilder = new TemplateBuilder(content);
        var template = templateBuilder.Build();
        
        // act
        string result = template();
        
        // verify
        Assert.Equal("ABC", result);
    }

    [Fact(DisplayName = "Multiline statement works")]
    public void MultilineStatementWorks()
    {
        string content = "{% yield return \"abc\"\n.ToUpper();\nyield return \"DEF\".ToLower(); %}";
        TemplateBuilder templateBuilder = new TemplateBuilder(content);
        var template = templateBuilder.Build();
        
        // act
        string result = template();
        
        // verify
        Assert.Equal("ABCdef", result);
    }

    [Fact(DisplayName = "Statement with newline and spaces at end works")]
    public void StatementWithNewlineAndSpacesAtEndWorks()
    {
        string content = "{% yield return \"abc\"\n.ToUpper();\n  %}";
        TemplateBuilder templateBuilder = new TemplateBuilder(content);
        var template = templateBuilder.Build();
        
        // act
        string result = template();
        
        // verify
        Assert.Equal("ABC", result);
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

    [Theory(DisplayName = $"Trying to escape the content element fails with a {nameof(SyntaxErrorException)}")]
    [InlineData("""{{ 42}"; yield return "escaped"; yield return $"{42 }}""")]
    [InlineData("""{{ 42"; yield return "escaped"; yield return "42 }}""")]
    [InlineData("""{{ 42"; yield return "escaped"; yield return @"42 }}""")]
    [InlineData(""""{{ 42"""; yield return "escaped"; yield return """42 }}"""")]
    public void CannotEscapeContentElement(string content)
    {
        // setup
        TemplateBuilder templateBuilder = new TemplateBuilder(content)
            .WithType<int>()
            .WithType<string>();

        // act and verify
        Assert.Throws<SyntaxErrorException>(() =>
        {
            var template = templateBuilder.Build();
            var result = template();
            _outputHelper.WriteLine($"No {nameof(SyntaxErrorException)} was thrown.\nTemplate result is: '{result}'");
            Assert.NotEqual("42escaped42", result);
        });
    }

    [Theory(DisplayName = $"Trying to escape the statement element fails with a {nameof(SyntaxErrorException)}")]
    [InlineData("{% yield return \"42\"; yield return Test(); }\n private static string Test() { return \"escaped\";\n %}")]
    public void CannotEscapeStatementElement(string content)
    {
        // setup
        TemplateBuilder templateBuilder = new TemplateBuilder(content)
            .WithType<int>()
            .WithType<string>();

        // act and verify
        Assert.Throws<SyntaxErrorException>(() =>
        {
            var template = templateBuilder.Build();
            var result = template();
            _outputHelper.WriteLine($"No {nameof(SyntaxErrorException)} was thrown.\nTemplate result is: '{result}'");
            Assert.NotEqual("42escaped", result);
        });
    }
}