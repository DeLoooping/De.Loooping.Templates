using De.Loooping.Templates.Core.Configuration;
using De.Loooping.Templates.Core.ContentReplacers;

namespace De.Loooping.Templates.Core.TemplateProcessors;

public class ValueObjectTemplateProcessor<T> : TemplateProcessorBase, ITemplateProcessor<T>
{
    private readonly IContentReplacer<T> _contentReplacer;
    
    public ValueObjectTemplateProcessor(IContentReplacer<T> contentReplacer)
        : this(contentReplacer, new TemplateProcessorConfiguration())
    {
    }

    public ValueObjectTemplateProcessor(IContentReplacer<T> contentReplacer, TemplateProcessorConfiguration configuration)
        : base(configuration)
    {
        _contentReplacer = contentReplacer;
    }

    public string Process(string template, T replacementValues)
    {
        string result = this.Process(template, (placeholderName, format) =>
            _contentReplacer.Replace(Configuration, placeholderName, format, replacementValues) ?? string.Empty);

        return result;
    }
}