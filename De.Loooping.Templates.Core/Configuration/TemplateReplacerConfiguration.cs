namespace De.Loooping.Templates.Core.Configuration;

public class TemplateReplacerConfiguration
{
    public string PlaceHolderNameRegex { get; set; } = @"[a-zA-Z_][a-zA-Z0-9_]*";
    public bool AllowFormatting { get; set; } = true;
    public bool AllowChildren { get; set; } = true;

    public string LeftDelimiter { get; set; } = "{";
    public string RightDelimiter { get; set; } = "}";

    public char ChildSeparator { get; set; } = '.';
}