using De.Loooping.Templates.Core.Configuration;
using De.Loooping.Templates.Core.ContentReplacers;

namespace De.Loooping.Templates.Core.TemplateProcessors;

public class TemplateProcessor: TemplateProcessorBase, ITemplateProcessor
{
    private readonly IContentReplacer _contentReplacer;
    
    public TemplateProcessor(IContentReplacer contentReplacer)
        : this(contentReplacer, new TemplateProcessorConfiguration())
    {
    }

    public TemplateProcessor(IContentReplacer contentReplacer, TemplateProcessorConfiguration configuration)
        : base(configuration)
    {
        _contentReplacer = contentReplacer;
    }
    
    public string Process(string template)
    {
        string result = this.Process(template, (placeholderName, format) =>
            _contentReplacer.Replace(Configuration, placeholderName, format) ?? string.Empty);

        return result;
    }
}