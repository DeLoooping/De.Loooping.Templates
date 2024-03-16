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

    public override string? StackTrace
    {
        get
        {
            return $"in template on line {Location.Line}, column {Location.Column}\n{base.StackTrace}";
        }
    }
}