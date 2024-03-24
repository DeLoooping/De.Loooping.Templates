using Microsoft.CodeAnalysis.CSharp;

namespace De.Loooping.Templates.Core.Configuration;

public class TemplateProcessorConfiguration: ITokenizerConfiguration
{
    public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.CSharp11;

    public string LeftContentDelimiter { get; set; } = "{{";
    public string RightContentDelimiter { get; set; } = "}}";
    public string ContentFormatDelimiter { get; set; } = ":";

    public string LeftStatementDelimiter { get; set; } = "{%";
    public string RightStatementDelimiter { get; set; } = "%}";

    public string LeftCommentDelimiter { get; set; } = "{#";
    public string RightCommentDelimiter { get; set; } = "#}";

    public string LeftCustomBlockDelimiter { get; set; } = "{$";
    public string RightCustomBlockDelimiter { get; set; } = "$}";
    public string CustomBlockIdentifierDelimiter { get; set; } = ":";
}