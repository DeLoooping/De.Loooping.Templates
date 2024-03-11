using De.Loooping.Templates.Core.Diagnostic;

namespace De.Loooping.Templates.Core.TemplateProcessors;

public class SyntaxErrorException: Exception
{
    public IReadOnlyList<Error> Errors { get; }

    public SyntaxErrorException(string message, IEnumerable<Error> errors)
        :base(message)
    {
        Errors = errors.ToList();
    }

}