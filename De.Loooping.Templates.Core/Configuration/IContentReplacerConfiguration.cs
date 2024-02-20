namespace De.Loooping.Templates.Core.Configuration;

public interface IContentReplacerConfiguration
{
    bool AllowFormatting { get; }
    bool AllowChildren { get; }
    char ChildSeparator { get; }
}