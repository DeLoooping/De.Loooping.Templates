namespace De.Loooping.Templates.Core.Configuration;

public class TemplateProcessorConfiguration: IContentReplacerConfiguration, ITokenizerConfiguration
{
    public string PlaceHolderNameRegex { get; init; } = @"[a-zA-Z_][a-zA-Z0-9_]*";
    public bool AllowFormatting { get; init; } = true;
    public bool AllowChildren { get; init; } = true;

    public string LeftContentDelimiter { get; init; } = "{{";
    public string RightContentDelimiter { get; init; } = "}}";

    public string LeftStatementDelimiter { get; init; } = "{%";
    public string RightStatementDelimiter { get; init; } = "%}";

    public string LeftCommentDelimiter { get; init; } = "{#";
    public string RightCommentDelimiter { get; init; } = "#}";

    public char ChildSeparator { get; init; } = '.';
}