namespace De.Loooping.Templates.Core.Diagnostic;

public class Error
{
    public Error(string message, CodeLocation location)
    {
        Message = message;
        Location = location;
    }

    public string Message { get; }
    public CodeLocation Location { get; }
}