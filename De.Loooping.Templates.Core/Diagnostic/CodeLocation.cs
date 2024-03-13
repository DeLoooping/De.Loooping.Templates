namespace De.Loooping.Templates.Core.Diagnostic;

/// <summary>
/// Represents a certain location inside the code
/// </summary>
public class CodeLocation: IEquatable<CodeLocation>
{
    public static CodeLocation Create(int row, int column)
    {
        return new CodeLocation(row, column);
    }
    
    /// <summary>
    /// Constructs a CodeLocation
    /// </summary>
    /// <param name="row">Zero-based row inside the code</param>
    /// <param name="column">Zero-based column inside the code</param>
    public CodeLocation(int row, int column)
    {
        Row = row;
        Column = column;
    }

    /// <summary>
    /// Zero-based row inside the code
    /// </summary>
    public int Row { get; }
    
    /// <summary>
    /// Zero-based column inside the code
    /// </summary>
    public int Column { get; }

    public bool Equals(CodeLocation? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Row == other.Row && Column == other.Column;
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
        return HashCode.Combine(Row, Column);
    }
}