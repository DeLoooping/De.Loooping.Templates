namespace De.Loooping.Templates.Core.Diagnostic;

/// <summary>
/// Represents a certain position inside the code
/// </summary>
public class CodePosition
{
    /// <summary>
    /// Constructs a CodePosition
    /// </summary>
    /// <param name="row">Zero-based row inside the code</param>
    /// <param name="column">Zero-based column inside the code</param>
    public CodePosition(int row, int column)
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
}