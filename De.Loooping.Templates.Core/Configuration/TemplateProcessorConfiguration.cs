using Microsoft.CodeAnalysis.CSharp;

namespace De.Loooping.Templates.Core.Configuration;

public class TemplateProcessorConfiguration: ITokenizerConfiguration
{
    public LanguageVersion LanguageVersion { get; init; } = LanguageVersion.CSharp12;
    public bool AllowFormatting { get; init; } = true;

    public string LeftContentDelimiter { get; init; } = "{{";
    public string RightContentDelimiter { get; init; } = "}}";
    public string ContentFormatDelimiter { get; init; } = ":";

    public string LeftStatementDelimiter { get; init; } = "{%";
    public string RightStatementDelimiter { get; init; } = "%}";

    public string LeftCommentDelimiter { get; init; } = "{#";
    public string RightCommentDelimiter { get; init; } = "#}";
}