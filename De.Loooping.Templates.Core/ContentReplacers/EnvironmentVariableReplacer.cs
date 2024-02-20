using System.Collections;
using System.Globalization;
using De.Loooping.Templates.Core.Configuration;

namespace De.Loooping.Templates.Core.ContentReplacers;

public class EnvironmentVariableReplacer: AbstractObjectPropertyReplacer
{
    public override string? Replace(IContentReplacerConfiguration configuration, string placeholderName, string? format)
    {
        IDictionary env = Environment.GetEnvironmentVariables();
        return GetObjectPropertyValue(configuration, placeholderName, format, env, CultureInfo.InvariantCulture);
    }
}