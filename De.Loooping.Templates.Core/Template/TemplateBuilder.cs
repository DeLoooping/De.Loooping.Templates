using System.Reflection;
using De.Loooping.Templates.Core.Configuration;
using De.Loooping.Templates.Core.Template.CustomBlocks;

namespace De.Loooping.Templates.Core.Template;

public sealed class TemplateBuilder<TDelegate> : TemplateBuilderBase<TDelegate>, IFluentTemplateBuilder<TemplateBuilder<TDelegate>>, IFluentTemplateBuilder, ITemplateBuilder
    where TDelegate : class, MulticastDelegate
{
    public TemplateBuilder(string template, TemplateProcessorConfiguration? configuration = null)
        : base(template, configuration)
    {
    }

    #region IFluentTemplateBuilder<TemplateBuilder<TDelegate>>
    
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

    public TemplateBuilder<TDelegate> WithCustomBlock(ICustomBlock customBlock, string? identifier = null)
    {
        AddCustomBlock(customBlock, identifier);
        return this;
    }

    #endregion

    #region IFluentTemplateBuilder

    IFluentTemplateBuilder IFluentTemplateBuilder.WithType(Type type)
    {
        return WithType(type);
    }

    IFluentTemplateBuilder IFluentTemplateBuilder.WithUsing(string @using)
    {
        return WithUsing(@using);
    }

    IFluentTemplateBuilder IFluentTemplateBuilder.WithReference(Assembly reference)
    {
        return WithReference(reference);
    }

    IFluentTemplateBuilder IFluentTemplateBuilder.WithType<T>()
    {
        return WithType<T>();
    }

    IFluentTemplateBuilder IFluentTemplateBuilder.WithCustomBlock(ICustomBlock customBlock, string? identifier)
    {
        return WithCustomBlock(customBlock, identifier);
    }

    #endregion
}

public sealed class TemplateBuilder : TemplateBuilderBase<Process>, IFluentTemplateBuilder<TemplateBuilder>, IFluentTemplateBuilder
{
    public TemplateBuilder(string template, TemplateProcessorConfiguration? configuration = null)
        : base(template, configuration)
    {
    }

    #region IFluentTemplateBuilder<TemplateBuilder>

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

    public TemplateBuilder WithCustomBlock(ICustomBlock customBlock, string? identifier = null)
    {
        AddCustomBlock(customBlock, identifier);
        return this;
    }
    
    #endregion
    
    #region IFluentTemplateBuilder

    IFluentTemplateBuilder IFluentTemplateBuilder.WithType(Type type)
    {
        return WithType(type);
    }

    IFluentTemplateBuilder IFluentTemplateBuilder.WithUsing(string @using)
    {
        return WithUsing(@using);
    }

    IFluentTemplateBuilder IFluentTemplateBuilder.WithReference(Assembly reference)
    {
        return WithReference(reference);
    }

    IFluentTemplateBuilder IFluentTemplateBuilder.WithType<T>()
    {
        return WithType<T>();
    }

    IFluentTemplateBuilder IFluentTemplateBuilder.WithCustomBlock(ICustomBlock customBlock, string? identifier)
    {
        return WithCustomBlock(customBlock, identifier);
    }
    
    #endregion
}