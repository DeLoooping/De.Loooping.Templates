using De.Loooping.Templates.Core.CodeMapping;

namespace De.Loooping.Templates.Core.Diagnostic;

internal static class DiagnosticExtensions
{
    public static Error ToError(this Microsoft.CodeAnalysis.Diagnostic diagnostic, CodeMapper codeMapper, int basePosition = 0)
    {
        int generatedCodeCharPosition = diagnostic.Location.SourceSpan.End + basePosition;
        CodeLocation startLocation = codeMapper.GetGeneratingCodeLocation(generatedCodeCharPosition);
        return new Error(diagnostic.GetMessage(), startLocation);
    }
}