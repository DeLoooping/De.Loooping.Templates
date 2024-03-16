using De.Loooping.Templates.Core.Diagnostic;

namespace De.Loooping.Templates.Core.Template;

public class SyntaxErrorException: Exception
{
    public IReadOnlyList<Error> Errors { get; }

    public SyntaxErrorException(string message, IEnumerable<Error> errors)
        :base(message)
    {
        Errors = errors.ToList();
    }

    public override string ToString()
    {
        return $"M:{Message}\n{String.Join("\n", Errors)}\n{base.ToString()}";
    }
}