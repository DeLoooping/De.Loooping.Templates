using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using De.Loooping.Templates.Core.Configuration;

namespace De.Loooping.Templates.Core;

public class TemplateReplacer
{
    public delegate string PlaceholderReplacer(string name, string? format);

    private readonly TemplateReplacerConfiguration _configuration;
    private readonly Regex _placeholderRegex;

    public TemplateReplacer(TemplateReplacerConfiguration configuration)
    {
        if (String.IsNullOrEmpty(configuration.LeftDelimiter))
        {
            throw new ArgumentException($"{nameof(configuration)}.{nameof(configuration.LeftDelimiter)} must not be null or empty.");
        }

        if (String.IsNullOrEmpty(configuration.RightDelimiter))
        {
            throw new ArgumentException($"{nameof(configuration)}.{nameof(configuration.LeftDelimiter)} must not be null or empty.");
        }

        if (!IsValidRegex(configuration.PlaceHolderNameRegex))
        {
            throw new ArgumentException($"{nameof(configuration)}.{nameof(configuration.PlaceHolderNameRegex)} must be a valid regular expression.");
        }
        
        _configuration = configuration;

        StringBuilder builder = new StringBuilder();
        builder.Append(Regex.Escape(_configuration.LeftDelimiter));
        builder.Append(@"\s*(?<PLACEHOLDERNAME>");
        if (_configuration.AllowChildren)
        {
            builder.Append($"({_configuration.PlaceHolderNameRegex})({Regex.Escape(_configuration.ChildSeparator.ToString())}({_configuration.PlaceHolderNameRegex}))*");
        }
        else
        {
            builder.Append($"({_configuration.PlaceHolderNameRegex})");
        }
        builder.Append(")");
        if (_configuration.AllowFormatting)
        {
            builder.Append(@"(\s*:(?<FORMAT>[^}]*))?");
        }
        builder.Append(@"\s*");
        builder.Append(Regex.Escape(_configuration.RightDelimiter));

        var pattern = builder.ToString();
        
        _placeholderRegex = new Regex(pattern, RegexOptions.Compiled);
    }
    
    private static bool IsValidRegex(string testPattern)
    {
        if (String.IsNullOrWhiteSpace(testPattern))
        {
            return false;
        }

        try
        {
            Regex.Match("", testPattern);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
    
    public string ReplacePlaceholders(string template, PlaceholderReplacer replacer)
    {
        string body = _placeholderRegex.Replace(template, match =>
        {
            string placeholderName = match.Groups["PLACEHOLDERNAME"].Value;
            string format = match.Groups["FORMAT"].Value;
            return replacer(placeholderName, format) ?? string.Empty;
        });

        return body;
    }

    public string ReplacePlaceholders<T>(string template, T @object, IFormatProvider? formatProvider = null)
    {
        return ReplacePlaceholders(template, (key, format) => 
            GetReplacementValue(key, format, @object, formatProvider) ?? string.Empty);
    }

    private string? GetReplacementValue<T>(string placeholderName, string? format, T @object, IFormatProvider? formatProvider)
    {
        string[] placeholderParts = _configuration.AllowChildren ? 
            placeholderName.Split(_configuration.ChildSeparator) : 
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
        
        var v = currentObject;
        if (v == null)
        {
            return null;
        }

        if (String.IsNullOrEmpty(format))
        {
            return String.Format(formatProvider, "{0}", v);
        }
        else
        {
            return String.Format(formatProvider, $"{{0:{format}}}", v);
        }
    }
}