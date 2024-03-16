using System.Text;
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

    public override string Message
    {
        get
        {
            StringBuilder sb = new();
            sb.AppendLine(base.Message);
            foreach (Error error in Errors)
            {
                sb.AppendLine($"- Error (line: {error.Location.Line}, column: {error.Location.Column}): {error.Message}");
            }

            return sb.ToString();
        }
    }
}