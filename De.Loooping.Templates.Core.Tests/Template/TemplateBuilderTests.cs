using De.Loooping.Templates.Core.Configuration;
using De.Loooping.Templates.Core.Diagnostic;
using De.Loooping.Templates.Core.Template;
using De.Loooping.Templates.Core.Template.CustomBlocks;
using De.Loooping.Templates.Core.Tests.Tools;
using Xunit.Abstractions;

namespace De.Loooping.Templates.Core.Tests.Template;

[Trait("TestCategory", "UnitTest")]
public class TemplateBuilderTests
{
    private class MyCustomBlock : ICustomBlock
    {
        public string DefaultIdentifier => "Test";
        public string Evaluate(string content)
        {
            throw new NotImplementedException();
        }
    }

    private readonly ITestOutputHelper _outputHelper;

    public TemplateBuilderTests(ITestOutputHelper outputHelper)
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

    [Fact(DisplayName = "Template implicitely adds references to input type assemblies")]
    public void TemplateAddsReferencesOfInputTypes()
    {
        // setup
        var counter = new Counter();
        string content = "{{ counter.CountUp() }}{{ counter.CountUp() }}";

        var templateBuilder = new TemplateBuilder<CounterDelegate>(content);

        // act
        var template = templateBuilder.Build();
        var result = template(counter);

        // verify
        Assert.Equal("01", result);
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

    [Fact(DisplayName = $"{nameof(TemplateBuilder)} respects a newline at the end of the template")]
    public void TemplateBuilderRespectsNewlineAtEnd()
    {
        // setup
        string content = "Start\n";
        TemplateBuilder templateBuilder = new TemplateBuilder(content)
            .WithType<int>();
        var template = templateBuilder.Build();

        // act
        string result = template();

        // verify
        Assert.Equal("Start\n", result);
    }
    
    public class CompilerErrorsAreAtTheRightLocationData: TestData<(string content, (int row, int column)[] errorLocations, bool allowAdditionalErrors)>
    {
        protected override IEnumerable<(string content, (int row, int column)[] errorLocations, bool allowAdditionalErrors)> GetData()
        {
            yield return (
                "{# a comment #}\n{%\nyield return \"a\";\nyield return \"b\";return \"c\";\n%}",
                new[] { (4, 24) },
                true
            );
        }
    }

    [Theory(DisplayName = $"Compiler errors are at the right location")]
    [ClassData(typeof(CompilerErrorsAreAtTheRightLocationData))]
    public void CompilerErrorsAreAtTheRightLocation(string content, (int row, int column)[] errorLocations, bool allowAdditionalErrors)
    {
        // setup
        TemplateBuilder templateBuilder = new TemplateBuilder(content)
            .WithType<int>()
            .WithType<string>();

        // act and verify
        var exception = Assert.Throws<CompilerErrorException>(() =>
        {
            templateBuilder.Build();
        });

        var expectedErrorLocations = errorLocations.Select(e => new CodeLocation(e.row, e.column)).ToList().ToHashSet();
        var actualErrorLocations = exception.Errors.Select(e => e.Location).ToHashSet();
        
        Assert.Superset(expectedErrorLocations, actualErrorLocations);
        if (!allowAdditionalErrors)
        {
            Assert.Subset(expectedErrorLocations, actualErrorLocations);
        }
    }
    
    public class RuntimeErrorsAreAtTheRightLocationData: TestData<(string content, int line, int column, Type? innerException)>
    {
        protected override IEnumerable<(string content, int line, int column, Type? innerException)> GetData()
        {
            yield return (
                "{%\nthrow new NotImplementedException(\"Test\");\n%}",
                2, 1,
                typeof(NotImplementedException)
            );
            yield return (
                "Start\n{{ 42/(new List<int>().Count) }}\nEnd",
                2, 2, // TODO: This case is a bit strange. Shouldn't it be (2,3)?  
                typeof(DivideByZeroException)
            );
        }
    }

    [Theory(DisplayName = $"{nameof(TemplateBuilder)} throws {nameof(RuntimeErrorException)} and returns correct error location")]
    [ClassData(typeof(RuntimeErrorsAreAtTheRightLocationData))]
    public void RuntimeErrorsAreAtTheRightLocation(string content, int line, int column, Type? innerException)
    {
        // setup
        TemplateBuilder templateBuilder = new TemplateBuilder(content);
        var template = templateBuilder.Build();

        // act and validate
        var exception = Assert.Throws<RuntimeErrorException>(() => template());
        
        Assert.Equal(line, exception.Location.Line);        
        Assert.Equal(column, exception.Location.Column);        
        Assert.Equal(innerException, exception.InnerException?.GetType());        
    }


    public class SomeType
    {
        public int Value => 42;
    }

    private delegate string GenericTypeTest(List<SomeType> things);

    [Fact(DisplayName = $"{nameof(TemplateBuilder)} also adds generic type parameters to references")]
    public void TemplateBuilderAlsoAddsGenericTypeParametersOnAddType()
    {
        // setup
        string content = "{{ String.Concat(things.Select(t => t.Value)) }}";
        var templateBuilder = new TemplateBuilder<GenericTypeTest>(content);
        var template = templateBuilder.Build();

        // act and validate
        var result = template(new List<SomeType> { new SomeType(), new SomeType() });
        Assert.Equal("4242", result);
    }

    [Fact(DisplayName = $"{nameof(TemplateBuilder)} throws with bad configuration")]
    public void TemplateBuilderThrowsWithBadConfiguration()
    {
        // setup
        string content = "Template";
        TemplateProcessorConfiguration configuration = new TemplateProcessorConfiguration()
        {
            LeftContentDelimiter = "{{{",
            LeftStatementDelimiter = "{{{{"
        };
        
        // act and validate
        var exception = Assert.Throws<ArgumentException>(() => new TemplateBuilder(content, configuration));
        _outputHelper.WriteLine(exception.ToString());
    }

    [Fact(DisplayName = $"{nameof(TemplateBuilder)} throws {nameof(SyntaxErrorException)} on unexpected end of file")]
    public void TemplateBuilderThrowsSyntaxErrorOnUnexpectedEndOfFile()
    {
        // setiup
        string content = "Line 1\nLine 2\nLine {{ 3";
        TemplateBuilder templateBuilder = new TemplateBuilder(content);
        
        // act and validate
        var exception = Assert.Throws<SyntaxErrorException>(() => templateBuilder.Build());
        
        Assert.Collection(exception.Errors,
            error =>
            {
                Assert.Equal(3, error.Location.Line);
                Assert.Equal(10, error.Location.Column);
            }
        );
    }

    [Fact(DisplayName = $"{nameof(TemplateBuilder)} adds to Usings when calling {nameof(TemplateBuilder.WithUsing)}(..)")]
    public void TemplateBuilderWithUsingAddsToUsings()
    {
        // setup
        var templateBuilder = new TemplateBuilder(String.Empty)
            .WithUsing("a.new.using");
        
        // verify
        Assert.Contains("a.new.using", templateBuilder.Usings);
    }

    [Fact(DisplayName = $"{nameof(TemplateBuilder)} adds to Usings when calling {nameof(TemplateBuilder.AddUsing)}(..)")]
    public void TemplateBuilderAddUsingAddsToUsings()
    {
        // setup
        var templateBuilder = new TemplateBuilder(String.Empty);
        templateBuilder.AddUsing("a.new.using");
        
        // verify
        Assert.Contains("a.new.using", templateBuilder.Usings);
    }

    [Fact(DisplayName = $"{nameof(TemplateBuilder)} adds to References when calling {nameof(TemplateBuilder.WithReference)}(..)")]
    public void TemplateBuilderWithReferenceAddsToReferences()
    {
        // setup
        var templateBuilder = new TemplateBuilder(String.Empty)
            .WithReference(GetType().Assembly);
        
        // verify
        Assert.Contains(GetType().Assembly, templateBuilder.References);
    }

    [Fact(DisplayName = $"{nameof(TemplateBuilder)} adds to References when calling {nameof(TemplateBuilder.AddReference)}(..)")]
    public void TemplateBuilderAddReferenceAddsToReferences()
    {
        // setup
        var templateBuilder = new TemplateBuilder(String.Empty);
        templateBuilder.AddReference(GetType().Assembly);
        
        // verify
        Assert.Contains(GetType().Assembly, templateBuilder.References);
    }

    [Fact(DisplayName = $"{nameof(TemplateBuilder)} adds to References and Usings when calling {nameof(TemplateBuilder.WithType)}(..)")]
    public void TemplateBuilderWithTypeAddsToReferencesAndUsings()
    {
        // setup
        var templateBuilder = new TemplateBuilder(String.Empty)
            .WithType(GetType());
        
        // verify
        Assert.Contains(GetType().Assembly, templateBuilder.References);
        Assert.Contains(GetType().Namespace!, templateBuilder.Usings);
    }

    [Fact(DisplayName = $"{nameof(TemplateBuilder)} adds to References and Usings when calling {nameof(TemplateBuilder.WithType)}<..>()")]
    public void TemplateBuilderGenericWithTypeAddsToReferencesAndUsings()
    {
        // setup
        var templateBuilder = new TemplateBuilder(String.Empty)
            .WithType<TemplateBuilderTests>();
        
        // verify
        Assert.Contains(GetType().Assembly, templateBuilder.References);
        Assert.Contains(GetType().Namespace!, templateBuilder.Usings);
    }

    [Fact(DisplayName = $"{nameof(TemplateBuilder)} adds to References and Usings when calling {nameof(TemplateBuilder.AddType)}(..)")]
    public void TemplateBuilderAddTypeAddsToReferencesAndUsings()
    {
        // setup
        var templateBuilder = new TemplateBuilder(String.Empty);
        templateBuilder.AddType(GetType());
        
        // verify
        Assert.Contains(GetType().Assembly, templateBuilder.References);
        Assert.Contains(GetType().Namespace!, templateBuilder.Usings);
    }

    [Fact(DisplayName = $"{nameof(TemplateBuilder)} adds to References and Usings when calling {nameof(TemplateBuilder.AddType)}<..>()")]
    public void TemplateBuilderGenericAddTypeAddsToReferencesAndUsings()
    {
        // setup
        var templateBuilder = new TemplateBuilder(String.Empty);
        templateBuilder.AddType<TemplateBuilderTests>();
        
        // verify
        Assert.Contains(GetType().Assembly, templateBuilder.References);
        Assert.Contains(GetType().Namespace!, templateBuilder.Usings);
    }

    [Fact(DisplayName = $"{nameof(TemplateBuilder)} adds to CustomBlocks when calling {nameof(TemplateBuilder.WithCustomBlock)}()")]
    public void TemplateBuilderWithCustomBlockAddsToCustomBlocks()
    {
        // setup
        var customBlock = new MyCustomBlock();
        var templateBuilder = new TemplateBuilder(String.Empty)
            .WithCustomBlock(customBlock);
        
        // verify
        Assert.Contains(customBlock, templateBuilder.CustomBlocks);
    }

    [Fact(DisplayName = $"{nameof(TemplateBuilder)} adds to CustomBlocks when calling {nameof(TemplateBuilder.AddCustomBlock)}()")]
    public void TemplateBuilderAddCustomBlockAddsToCustomBlocks()
    {
        // setup
        var customBlock = new MyCustomBlock();
        var templateBuilder = new TemplateBuilder(String.Empty);
        templateBuilder.AddCustomBlock(customBlock);
        
        // verify
        Assert.Contains(customBlock, templateBuilder.CustomBlocks);
    }

    [Fact(DisplayName = $"{nameof(TemplateBuilder)}<> adds to Usings when calling {nameof(TemplateBuilder.WithUsing)}(..)")]
    public void GenericTemplateBuilderWithUsingAddsToUsings()
    {
        // setup
        var templateBuilder = new TemplateBuilder<Process>(String.Empty)
            .WithUsing("a.new.using");
        
        // verify
        Assert.Contains("a.new.using", templateBuilder.Usings);
    }
    
    [Fact(DisplayName = $"{nameof(TemplateBuilder)}<> adds to Usings when calling {nameof(TemplateBuilder.AddUsing)}(..)")]
    public void GenericTemplateBuilderAddUsingAddsToUsings()
    {
        // setup
        var templateBuilder = new TemplateBuilder<Process>(String.Empty);
        templateBuilder.AddUsing("a.new.using");
        
        // verify
        Assert.Contains("a.new.using", templateBuilder.Usings);
    }
    
    [Fact(DisplayName = $"{nameof(TemplateBuilder)}<> adds to References when calling {nameof(TemplateBuilder.WithReference)}(..)")]
    public void GenericTemplateBuilderWithReferenceAddsToReferences()
    {
        // setup
        var templateBuilder = new TemplateBuilder<Process>(String.Empty)
            .WithReference(GetType().Assembly);
        
        // verify
        Assert.Contains(GetType().Assembly, templateBuilder.References);
    }

    [Fact(DisplayName = $"{nameof(TemplateBuilder)}<> adds to References when calling {nameof(TemplateBuilder.AddReference)}(..)")]
    public void GenericTemplateBuilderAddReferenceAddsToReferences()
    {
        // setup
        var templateBuilder = new TemplateBuilder<Process>(String.Empty);
        templateBuilder.AddReference(GetType().Assembly);
        
        // verify
        Assert.Contains(GetType().Assembly, templateBuilder.References);
    }

    [Fact(DisplayName = $"{nameof(TemplateBuilder)}<> adds to References and Usings when calling {nameof(TemplateBuilder.WithType)}(..)")]
    public void GenericTemplateBuilderWithTypeAddsToReferencesAndUsings()
    {
        // setup
        var templateBuilder = new TemplateBuilder<Process>(String.Empty)
            .WithType(GetType());
        
        // verify
        Assert.Contains(GetType().Assembly, templateBuilder.References);
        Assert.Contains(GetType().Namespace!, templateBuilder.Usings);
    }

    [Fact(DisplayName = $"{nameof(TemplateBuilder)}<> adds to References and Usings when calling {nameof(TemplateBuilder.WithType)}<..>()")]
    public void GenericTemplateBuilderGenericWithTypeAddsToReferencesAndUsings()
    {
        // setup
        var templateBuilder = new TemplateBuilder<Process>(String.Empty)
            .WithType<TemplateBuilderTests>();
        
        // verify
        Assert.Contains(GetType().Assembly, templateBuilder.References);
        Assert.Contains(GetType().Namespace!, templateBuilder.Usings);
    }

    [Fact(DisplayName = $"{nameof(TemplateBuilder)}<> adds to References and Usings when calling {nameof(TemplateBuilder.AddType)}(..)")]
    public void GenericTemplateBuilderAddTypeAddsToReferencesAndUsings()
    {
        // setup
        var templateBuilder = new TemplateBuilder<Process>(String.Empty);
        templateBuilder.AddType(GetType());
        
        // verify
        Assert.Contains(GetType().Assembly, templateBuilder.References);
        Assert.Contains(GetType().Namespace!, templateBuilder.Usings);
    }

    [Fact(DisplayName = $"{nameof(TemplateBuilder)}<> adds to References and Usings when calling {nameof(TemplateBuilder.AddType)}<..>()")]
    public void GenericTemplateBuilderGenericAddTypeAddsToReferencesAndUsings()
    {
        // setup
        var templateBuilder = new TemplateBuilder<Process>(String.Empty);
        templateBuilder.AddType<TemplateBuilderTests>();
        
        // verify
        Assert.Contains(GetType().Assembly, templateBuilder.References);
        Assert.Contains(GetType().Namespace!, templateBuilder.Usings);
    }
 
    [Fact(DisplayName = $"{nameof(TemplateBuilder)}<> adds to CustomBlocks when calling {nameof(TemplateBuilder.WithCustomBlock)}()")]
    public void GenericTemplateBuilderWithCustomBlockAddsToCustomBlocks()
    {
        // setup
        var customBlock = new MyCustomBlock();
        var templateBuilder = new TemplateBuilder<Process>(String.Empty)
            .WithCustomBlock(customBlock);
        
        // verify
        Assert.Contains(customBlock, templateBuilder.CustomBlocks);
    }

    [Fact(DisplayName = $"{nameof(TemplateBuilder)}<> adds to CustomBlocks when calling {nameof(TemplateBuilder.AddCustomBlock)}()")]
    public void GenericTemplateBuilderAddCustomBlockAddsToCustomBlocks()
    {
        // setup
        var customBlock = new MyCustomBlock();
        var templateBuilder = new TemplateBuilder<Process>(String.Empty);
        templateBuilder.AddCustomBlock(customBlock);
        
        // verify
        Assert.Contains(customBlock, templateBuilder.CustomBlocks);
    }
}