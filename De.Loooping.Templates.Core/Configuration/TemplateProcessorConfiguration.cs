namespace De.Loooping.Templates.Core.Configuration;

public class TemplateProcessorConfiguration: IContentReplacerConfiguration
{
    public string PlaceHolderNameRegex { get; init; } = @"[a-zA-Z_][a-zA-Z0-9_]*";
    public bool AllowFormatting { get; init; } = true;
    public bool AllowChildren { get; init; } = true;

    public string LeftDelimiter { get; init; } = "{{";
    public string RightDelimiter { get; init; } = "}}";

    public char ChildSeparator { get; init; } = '.';
}