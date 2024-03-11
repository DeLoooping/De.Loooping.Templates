namespace De.Loooping.Templates.Core.Diagnostic;

public class Error
{
    public Error(string message, CodePosition position)
    {
        Message = message;
        Position = position;
    }

    public string Message { get; }
    public CodePosition Position { get; }
}