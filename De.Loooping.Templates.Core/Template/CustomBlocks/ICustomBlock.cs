namespace De.Loooping.Templates.Core.Template.CustomBlocks;

public interface ICustomBlock
{
    public string DefaultIdentifier { get; }

    public string Evaluate(string content);
}