using System.Reflection;
using De.Loooping.Templates.Core.Configuration;
using De.Loooping.Templates.Core.Template.CustomBlocks;

namespace De.Loooping.Templates.Core.Template;

public interface ITemplateBuilder
{
    TemplateProcessorConfiguration Configuration { get; }
    
    void AddType<T>();
    void AddType(Type type);
    void AddUsing(string @using);
    void AddReference(Assembly reference);
    void AddCustomBlock(ICustomBlock customBlock, string? identifier = null);
}