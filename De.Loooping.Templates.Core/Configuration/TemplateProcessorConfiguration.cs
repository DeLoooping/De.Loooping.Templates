using Microsoft.CodeAnalysis.CSharp;

namespace De.Loooping.Templates.Core.Configuration;

public class TemplateProcessorConfiguration: ITokenizerConfiguration, ICodeGeneratorConfiguration
{
    /// <summary>
    /// Specifies the version of C# features that can be used in content or statement blocks.
    /// Default is C#11. 
    /// </summary>
    public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.CSharp11;

    /// <summary>
    /// If true, content blocks are evaluated and replaced by the evaluated result.
    /// If false, content blocks are left as is.
    /// Default is true.
    /// </summary>
    public bool EvaluateContentBlocks { get; set; } = true;
    /// <summary>
    /// Specifies the left delimiter of content blocks.
    /// Defaults to "{{".
    /// </summary>
    public string LeftContentDelimiter { get; set; } = "{{";
    /// <summary>
    /// Specifies the right delimiter of content blocks.
    /// Defaults to "}}".
    /// </summary>
    public string RightContentDelimiter { get; set; } = "}}";
    /// <summary>
    /// Specifies the delimiter between value and format inside content blocks.
    /// Defaults to ":".
    /// </summary>
    public string ContentFormatDelimiter { get; set; } = ":";

    /// <summary>
    /// If true, statement blocks are evaluated and replaced by the evaluated result.
    /// If false, statement blocks are left as is.
    /// Default is true.
    /// </summary>
    public bool EvaluateStatementBlocks { get; set; } = true;
    /// <summary>
    /// Specifies the left delimiter of statement blocks.
    /// Defaults to "{%".
    /// </summary>
    public string LeftStatementDelimiter { get; set; } = "{%";
    /// <summary>
    /// Specifies the right delimiter of statement blocks.
    /// Defaults to "%}".
    /// </summary>
    public string RightStatementDelimiter { get; set; } = "%}";

    /// <summary>
    /// If true, comment blocks are removed from the resulting string.
    /// If false, comment blocks are left as is.
    /// Default is true.
    /// </summary>
    public bool RemoveCommentBlocks { get; set; } = true;
    /// <summary>
    /// Specifies the left delimiter of comment blocks.
    /// Defaults to "{#".
    /// </summary>
    public string LeftCommentDelimiter { get; set; } = "{#";
    /// <summary>
    /// Specifies the right delimiter of comment blocks.
    /// Defaults to "#}".
    /// </summary>
    public string RightCommentDelimiter { get; set; } = "#}";

    /// <summary>
    /// Specifies the left delimiter of custom blocks.
    /// Defaults to "{$".
    /// </summary>
    public string LeftCustomBlockDelimiter { get; set; } = "{$";
    /// <summary>
    /// Specifies the right delimiter of custom blocks.
    /// Defaults to "$}".
    /// </summary>
    public string RightCustomBlockDelimiter { get; set; } = "$}";
    /// <summary>
    /// Specifies the delimiter between identifier and content inside custom blocks.
    /// Defaults to ":".
    /// </summary>
    public string CustomBlockIdentifierDelimiter { get; set; } = ":";
}