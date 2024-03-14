using De.Loooping.Templates.Core.Diagnostic;

namespace De.Loooping.Templates.Core.Template;

public class CompilerErrorException: Exception
{
    public IReadOnlyList<Error> Errors { get; }

    public CompilerErrorException(string message, IEnumerable<Error> errors)
        :base(message)
    {
        Errors = errors.ToList();
    }
}