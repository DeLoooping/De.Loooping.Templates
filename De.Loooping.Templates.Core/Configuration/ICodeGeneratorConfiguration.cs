namespace De.Loooping.Templates.Core.Configuration;

public interface ICodeGeneratorConfiguration
{
    bool EvaluateContentBlocks { get; }
    bool EvaluateStatementBlocks { get; }
    bool RemoveCommentBlocks { get; }
}