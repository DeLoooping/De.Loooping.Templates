namespace De.Loooping.Templates.Core.Template.CustomBlocks;

public class EnvironmentVariableContentBlock: ICustomBlock
{
    private readonly string _defaultValueDelimiter;

    public EnvironmentVariableContentBlock(string? defaultValueDelimiter = null)
    {
        _defaultValueDelimiter = defaultValueDelimiter ?? ":";
    }
    
    public string DefaultIdentifier => "ENV";
    public string Evaluate(string content)
    {
        string[] parts = content.Split(_defaultValueDelimiter, 2);
        string variableName = parts[0];
        string defaultValue = parts.Length > 1 ? parts[1] : String.Empty;

        if (String.IsNullOrEmpty(variableName))
        {
            return defaultValue;
        }

        string? environmentVariableContent = Environment.GetEnvironmentVariable(variableName);
        string result = !String.IsNullOrEmpty(environmentVariableContent) ? environmentVariableContent : defaultValue;
        return result;
    }
}