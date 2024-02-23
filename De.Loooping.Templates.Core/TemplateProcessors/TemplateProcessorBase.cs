using System.Text;
using System.Text.RegularExpressions;
using De.Loooping.Templates.Core.Configuration;

namespace De.Loooping.Templates.Core.TemplateProcessors;

public abstract class TemplateProcessorBase
{
    protected delegate string? ReplaceSinglePlaceHolder(string placeholderName, string? format);

    private readonly Regex _placeholderRegex;

    protected TemplateProcessorConfiguration Configuration { get; }
    
    public TemplateProcessorBase()
        : this(new TemplateProcessorConfiguration())
    {
    }

    public TemplateProcessorBase(TemplateProcessorConfiguration configuration)
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

        Configuration = configuration;

        StringBuilder builder = new StringBuilder();
        builder.Append(Regex.Escape(Configuration.LeftDelimiter));
        builder.Append(@"\s*(?<PLACEHOLDERNAME>");
        if (Configuration.AllowChildren)
        {
            builder.Append($"({Configuration.PlaceHolderNameRegex})({Regex.Escape(Configuration.ChildSeparator.ToString())}({Configuration.PlaceHolderNameRegex}))*");
        }
        else
        {
            builder.Append($"({Configuration.PlaceHolderNameRegex})");
        }
        builder.Append(")");
        if (Configuration.AllowFormatting)
        {
            builder.Append(@"(\s*:(?<FORMAT>[^}]*))?");
        }
        builder.Append(@"\s*");
        builder.Append(Regex.Escape(Configuration.RightDelimiter));

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
    
    protected string Process(string template, ReplaceSinglePlaceHolder replace)
    {
        string result = _placeholderRegex.Replace(template, match =>
        {
            string placeholderName = match.Groups["PLACEHOLDERNAME"].Value;
            Group formatGroup = match.Groups["FORMAT"];
            string? format = formatGroup.Success ? formatGroup.Value : null;
            return replace(placeholderName, format) ?? string.Empty;
        });

        return result;
    }

}