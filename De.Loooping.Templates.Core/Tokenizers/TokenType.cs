namespace De.Loooping.Templates.Core.Tokenizers;

internal enum TokenType
{
    LeftContentDelimiter,
    RightContentDelimiter,
    
    LeftStatementDelimiter,
    RightStatementDelimiter,
    
    LeftCommentDelimiter,
    RightCommentDelimiter,

    Literal,
    CSharp,
}