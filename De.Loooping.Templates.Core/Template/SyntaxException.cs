namespace De.Loooping.Templates.Core.TemplateProcessors;

public class SyntaxException: Exception
{
    public SyntaxException() 
    {
    }

    public SyntaxException(string message) 
        : base(message)
    {
    }
}