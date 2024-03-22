namespace De.Loooping.Templates.Core.Tokenizers;

internal enum TokenType
{
    LeftContentDelimiter,
    RightContentDelimiter,
    ContentFormatDelimiter,
    
    LeftStatementDelimiter,
    RightStatementDelimiter,
    
    LeftCommentDelimiter,
    RightCommentDelimiter,
    
    LeftCustomBlockDelimiter,
    RightCustomBlockDelimiter,
    CustomBlockIdentifierDelimiter,

    Literal,
    CSharp,
    Identifier
}