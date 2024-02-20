using System.Globalization;
using De.Loooping.Templates.Core.Configuration;

namespace De.Loooping.Templates.Core.ContentReplacers;

public class ObjectPropertyReplacer: AbstractObjectPropertyReplacer
{
    private readonly object _replacementValues;
    private readonly IFormatProvider _formatProvider;

    public ObjectPropertyReplacer(object replacementValues, IFormatProvider? formatProvider = null)
    {
        _replacementValues = replacementValues;
        _formatProvider = formatProvider ?? CultureInfo.InvariantCulture;
    }
    
    public override string? Replace(IContentReplacerConfiguration configuration, string placeholderName, string? format)
    {
        return GetObjectPropertyValue(configuration, placeholderName, format, _replacementValues, _formatProvider);
    }
}