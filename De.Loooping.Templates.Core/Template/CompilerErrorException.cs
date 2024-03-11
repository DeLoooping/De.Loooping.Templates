using De.Loooping.Templates.Core.Diagnostic;

namespace De.Loooping.Templates.Core.TemplateProcessors;

public class CompilerErrorException: Exception
{
    public IReadOnlyList<string> Errors { get; }

    public CompilerErrorException(string message, IEnumerable<string> errors)
        :base(message)
    {
        Errors = errors.ToList();
    }
}