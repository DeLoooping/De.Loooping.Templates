using De.Loooping.Templates.Core.Diagnostic;

namespace De.Loooping.Templates.Core.TemplateProcessors;

public class SyntaxErrorException: Exception
{
    public IReadOnlyList<string> Errors { get; }

    public SyntaxErrorException(string message, IEnumerable<string> errors)
        :base(message)
    {
        Errors = errors.ToList();
    }

}