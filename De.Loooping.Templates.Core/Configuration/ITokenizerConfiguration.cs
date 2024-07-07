namespace De.Loooping.Templates.Core.Configuration;

public interface ITokenizerConfiguration
{
    string LeftContentDelimiter { get; }
    string RightContentDelimiter { get; }
    string ContentFormatDelimiter { get; }

    string LeftStatementDelimiter { get; }
    string RightStatementDelimiter { get; }

    string LeftCommentDelimiter { get; }
    string RightCommentDelimiter { get; }
    
    string LeftCustomBlockDelimiter { get; }
    string RightCustomBlockDelimiter { get; }
    string CustomBlockIdentifierDelimiter { get; }
}