using De.Loooping.Templates.Core.Diagnostic;
using De.Loooping.Templates.Core.Template;
using De.Loooping.Templates.Core.Tests.Tools;
using Xunit.Abstractions;

namespace De.Loooping.Templates.Core.Tests.Template;

[Trait("TestCategory", "UnitTest")]
public class StatementBlockTests
{
    private readonly ITestOutputHelper _outputHelper;

    public StatementBlockTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
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
    
    public class CannotEscapeStatementElementData: TestData<(string content, (int row, int column)[] errorLocations, bool allowAdditionalErrors)>
    {
        protected override IEnumerable<(string content, (int row, int column)[] errorLocations, bool allowAdditionalErrors)> GetData()
        {
            yield return (
                "{% yield return \"42\"; yield return Test(); }\n private static string Test() { return \"escaped\";\n %}",
                new[] { (1, 44) },
                true
            );
            yield return (
                "{% yield return \"42\"; yield return Test(); }\n static string Test() { return \"escaped\";\n %}",
                new[] { (1, 44) },
                true
            );
            yield return (
                "{% yield return \"42\"; }\n string Test => { return \"escaped\";\n %}",
                new[] { (1, 23) },
                true
            );
        }
    }

    [Theory(DisplayName = $"Trying to escape the statement element fails with a {nameof(SyntaxErrorException)}")]
    [ClassData(typeof(CannotEscapeStatementElementData))]
    public void CannotEscapeStatementElement(string content, (int row, int column)[] errorLocations, bool allowAdditionalErrors)
    {
        // setup
        TemplateBuilder templateBuilder = new TemplateBuilder(content)
            .WithType<int>()
            .WithType<string>();

        // act and verify
        var exception = Assert.Throws<SyntaxErrorException>(() =>
        {
            var template = templateBuilder.Build();
            var result = template();
            _outputHelper.WriteLine($"No {nameof(SyntaxErrorException)} was thrown.\nTemplate result is: '{result}'");
            Assert.NotEqual("42escaped", result);
        });

        var expectedErrorLocations = errorLocations.Select(e => new CodeLocation(e.row, e.column)).ToList().ToHashSet();
        var actualErrorLocations = exception.Errors.Select(e => e.Location).ToHashSet();
        
        Assert.Superset(expectedErrorLocations, actualErrorLocations);
        if (!allowAdditionalErrors)
        {
            Assert.Subset(expectedErrorLocations, actualErrorLocations);
        }
    }

}