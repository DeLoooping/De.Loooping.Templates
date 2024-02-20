using De.Loooping.Templates.Core.Configuration;

namespace De.Loooping.Templates.Core.ContentReplacers;

public interface IContentReplacer
{
    string? Replace(IContentReplacerConfiguration configuration, string placeholderName, string? format);
}

public interface IContentReplacer<in T>
{
    string? Replace(IContentReplacerConfiguration configuration, string placeholderName, string? format, T replacementValues);
}
