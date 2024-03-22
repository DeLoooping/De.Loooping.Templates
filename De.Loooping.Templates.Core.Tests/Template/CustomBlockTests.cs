using De.Loooping.Templates.Core.Template;
using Xunit.Abstractions;

namespace De.Loooping.Templates.Core.Tests.Template;

[Trait("TestCategory", "UnitTest")]
public class CustomBlockTests
{
    private readonly ITestOutputHelper _outputHelper;

    public CustomBlockTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    private class MyCustomBlock : ICustomBlock
    {
        private readonly string _identifier;

        public MyCustomBlock(string identifier = "Custom")
        {
            _identifier = identifier;
        }
        
        public string DefaultIdentifier => _identifier;
        public string Evaluate(string content)
        {
            return $"[{content}]";
        }
    }
    
    [Fact(DisplayName = "Custom blocks work")]
    public void CustomBlocksWork()
    {
        // setup
        string content = "Prefix{$Custom: my custom block $}Suffix";
        TemplateBuilder templateBuilder = new TemplateBuilder(content);
        templateBuilder.AddCustomBlock(new MyCustomBlock());
        var template = templateBuilder.Build();
        
        // act
        string result = template();
        
        // verify
        Assert.Equal("Prefix[ my custom block ]Suffix", result);
    }

    [Fact(DisplayName = "Custom blocks work")]
    public void CustomBlocksWithChangedIdentifierWork()
    {
        // setup
        string content = "Prefix{$OtherIdentifier: my custom block $}Suffix";
        TemplateBuilder templateBuilder = new TemplateBuilder(content);
        templateBuilder.AddCustomBlock(new MyCustomBlock(), "OtherIdentifier");
        var template = templateBuilder.Build();

        // act
        string result = template();
        
        // verify
        Assert.Equal("Prefix[ my custom block ]Suffix", result);
    }

    [Fact(DisplayName = "Custom block result cannot escape the source code")]
    public void CustomBlockResultCannotEscapeTheCode()
    {
        // setup
        string content = "Prefix{$Custom: \"; throw new Exception(\"escaped! $}Suffix";
        TemplateBuilder templateBuilder = new TemplateBuilder(content);
        templateBuilder.AddCustomBlock(new MyCustomBlock());
        var template = templateBuilder.Build();
        
        // act
        string result = template();
        
        // verify
        Assert.Equal("Prefix[ \"; throw new Exception(\"escaped! ]Suffix", result);
    }

    [Fact(DisplayName = "Adding custom blocks with same identifier throws")]
    public void AddingCustomBlocksWithSameIdentifierThrows()
    {
        // setup
        string content = "Prefix{{$Custom: my custom block $}}Suffix";
        TemplateBuilder templateBuilder = new TemplateBuilder(content);
        templateBuilder.AddCustomBlock(new MyCustomBlock());
        
        // verify
        Assert.Throws<ArgumentException>(() => templateBuilder.AddCustomBlock(new MyCustomBlock()));
    }

    [Fact(DisplayName = "Adding custom blocks with same identifier throws 2")]
    public void AddingCustomBlocksWithSameIdentifierThrows2()
    {
        // setup
        TemplateBuilder templateBuilder = new TemplateBuilder(String.Empty);
        templateBuilder.AddCustomBlock(new MyCustomBlock("Custom"));
        
        // verify
        Assert.Throws<ArgumentException>(() => templateBuilder.AddCustomBlock(new MyCustomBlock(), "Custom"));
    }

    [Fact(DisplayName = "Adding custom blocks with whitespace in their identifier throws")]
    public void AddingCustomBlocksWithWhitespaceInIdentifierThrows()
    {
        // setup
        TemplateBuilder templateBuilder = new TemplateBuilder(String.Empty);
        
        // verify
        Assert.Throws<ArgumentException>(() => templateBuilder.AddCustomBlock(new MyCustomBlock("My Identifier")));
    }

    [Fact(DisplayName = "Adding custom blocks with whitespace in their identifier throws")]
    public void AddingCustomBlocksWithWhitespaceInIdentifierThrows2()
    {
        // setup
        TemplateBuilder templateBuilder = new TemplateBuilder(String.Empty);
        
        // verify
        Assert.Throws<ArgumentException>(() => templateBuilder.AddCustomBlock(new MyCustomBlock(), "My Identifier"));
    }

}