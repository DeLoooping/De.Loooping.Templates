using System.Reflection;

namespace De.Loooping.Templates.Core.Template;

public interface IFluentTemplateBuilder<TTemplateBuilder>
{
    TTemplateBuilder WithType<T>();
    TTemplateBuilder WithType(Type type);
    TTemplateBuilder WithUsing(string @using);
    TTemplateBuilder WithReference(Assembly reference);
}