using System.Reflection;
using De.Loooping.Templates.Core.Configuration;

namespace De.Loooping.Templates.Core.Template;

public sealed class TemplateBuilder<TDelegate> : TemplateBuilderBase<TDelegate>, IFluentTemplateBuilder<TemplateBuilder<TDelegate>>
    where TDelegate : class, MulticastDelegate
{
    public TemplateBuilder(string template, TemplateProcessorConfiguration? configuration = null)
        : base(template, configuration)
    {
    }

    public TemplateBuilder<TDelegate> WithType<T>()
    {
        AddType<T>();
        return this;
    }

    public TemplateBuilder<TDelegate> WithType(Type type)
    {
        AddType(type);
        return this;
    }

    public TemplateBuilder<TDelegate> WithUsing(string @using)
    {
        AddUsing(@using);
        return this;
    }

    public TemplateBuilder<TDelegate> WithReference(Assembly reference)
    {
        AddReference(reference);
        return this;
    }
}

public sealed class TemplateBuilder : TemplateBuilderBase<Process>, IFluentTemplateBuilder<TemplateBuilder>
{
    public TemplateBuilder(string template, TemplateProcessorConfiguration? configuration = null)
        : base(template, configuration)
    {
    }

    public TemplateBuilder WithType<T>()
    {
        AddType<T>();
        return this;
    }

    public TemplateBuilder WithType(Type type)
    {
        AddType(type);
        return this;
    }

    public TemplateBuilder WithUsing(string @using)
    {
        AddUsing(@using);
        return this;
    }

    public TemplateBuilder WithReference(Assembly reference)
    {
        AddReference(reference);
        return this;
    }
}