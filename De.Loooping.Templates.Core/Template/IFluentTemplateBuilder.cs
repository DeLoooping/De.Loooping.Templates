using System.Reflection;

namespace De.Loooping.Templates.Core.Template;

public interface IFluentTemplateBuilder<out TTemplateBuilder>: ITemplateBuilder
{
    TTemplateBuilder WithType<T>();
    TTemplateBuilder WithType(Type type);
    TTemplateBuilder WithUsing(string @using);
    TTemplateBuilder WithReference(Assembly reference);
}

public interface IFluentTemplateBuilder: ITemplateBuilder
{
    IFluentTemplateBuilder WithType<T>();
    IFluentTemplateBuilder WithType(Type type);
    IFluentTemplateBuilder WithUsing(string @using);
    IFluentTemplateBuilder WithReference(Assembly reference);
}