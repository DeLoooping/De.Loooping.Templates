namespace De.Loooping.Templates.Core.Template;

public interface ICustomBlock
{
    public string DefaultIdentifier { get; }

    public string Evaluate(string content);
}