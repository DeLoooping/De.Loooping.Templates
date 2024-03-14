using De.Loooping.Templates.Core.Diagnostic;

namespace De.Loooping.Templates.Core.Template;

public class RuntimeErrorException: Exception
{
    public CodeLocation Location { get; }

    public RuntimeErrorException(string message, CodeLocation location, Exception innerException)
        :base(message, innerException)
    {
        Location = location;
    }
}