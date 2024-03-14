namespace De.Loooping.Templates.Core.Diagnostic;

/// <summary>
/// Represents a certain location inside the code
/// </summary>
public class CodeLocation: IEquatable<CodeLocation>
{
    /// <summary>
    /// Constructs a CodeLocation
    /// </summary>
    /// <param name="line">One-based row inside the code</param>
    /// <param name="column">One-based column inside the code</param>
    public CodeLocation(int line, int column)
    {
        Line = line;
        Column = column;
    }

    /// <summary>
    /// One-based line number inside the code
    /// </summary>
    public int Line { get; }
    
    /// <summary>
    /// One-based column inside the code
    /// </summary>
    public int Column { get; }

    public bool Equals(CodeLocation? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Line == other.Line && Column == other.Column;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((CodeLocation)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Line, Column);
    }

    public override string ToString()
    {
        return $"(line: {Line}, column: {Column})";
    }
}