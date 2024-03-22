namespace De.Loooping.Templates.Core.Template.CustomBlocks;

public class EnvironmentVariableContentBlock: ICustomBlock
{
    public string DefaultIdentifier => "ENV";
    public string Evaluate(string content)
    {
        return Environment.GetEnvironmentVariable(content) ?? String.Empty;
    }
}