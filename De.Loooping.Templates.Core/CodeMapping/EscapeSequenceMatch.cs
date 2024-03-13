namespace De.Loooping.Templates.Core.CodeMapping;

internal class EscapeSequenceMatch
{
    public required bool Success { get; init; }
    public required string? EscapedSequence { get; init; }
    public required string? UnescapedSequence { get; init; }
}