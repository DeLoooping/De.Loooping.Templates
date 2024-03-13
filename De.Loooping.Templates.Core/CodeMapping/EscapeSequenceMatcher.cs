using System.Text.RegularExpressions;

namespace De.Loooping.Templates.Core.CodeMapping;

internal class EscapeSequenceMatcher
{
    private readonly Regex _escapeSequenceRegex;
    private readonly MatchEvaluator _unescaper;

    public EscapeSequenceMatcher(Regex escapeSequenceRegex, MatchEvaluator unescaper)
    {
        _escapeSequenceRegex = escapeSequenceRegex;
        _unescaper = unescaper;
    }
        
    public EscapeSequenceMatch Match(string text, int startAt)
    {
        var match = _escapeSequenceRegex.Match(text, startAt);
        if (!match.Success)
        {
            return new EscapeSequenceMatch()
            {
                Success = false,
                UnescapedSequence = null,
                EscapedSequence = null
            };
        }

        return new EscapeSequenceMatch()
        {
            Success = true,
            UnescapedSequence = _unescaper(match),
            EscapedSequence = match.Value
        };
    }
}
