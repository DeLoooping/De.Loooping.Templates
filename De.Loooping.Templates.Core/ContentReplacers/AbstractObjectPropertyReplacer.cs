using System.Collections;
using De.Loooping.Templates.Core.Configuration;

namespace De.Loooping.Templates.Core.ContentReplacers;

public abstract class AbstractObjectPropertyReplacer : IContentReplacer
{
    public abstract string? Replace(IContentReplacerConfiguration configuration, string placeholderName, string? format);
    
    protected string? GetObjectPropertyValue<T>(IContentReplacerConfiguration configuration, string placeholderName, string? format, T @object, IFormatProvider? formatProvider)
    {
        string[] placeholderParts = configuration.AllowChildren ? 
            placeholderName.Split(configuration.ChildSeparator) : 
            new[] { placeholderName };

        object? currentObject = @object;
        foreach (string part in placeholderParts)
        {
            if (currentObject == null)
            {
                break; 
            }

            if (currentObject is IDictionary dictionary)
            {
                currentObject = dictionary[part];
            }
            else
            {
                currentObject = currentObject.GetType().GetProperty(part)?.GetValue(currentObject);
            }
        }
        
        object? value = currentObject;
        if (value == null)
        {
            return null;
        }

        if (String.IsNullOrEmpty(format) || !configuration.AllowFormatting)
        {
            return String.Format(formatProvider, "{0}", value);
        }
        else
        {
            return String.Format(formatProvider, $"{{0:{format}}}", value);
        }
    }
}