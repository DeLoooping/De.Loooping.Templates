using De.Loooping.Templates.Core.Template;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace De.Loooping.Templates.Configuration;

public class JsonTemplateConfigurationSource<TDelegate> : JsonConfigurationSource
    where TDelegate : class, MulticastDelegate
{
    public Func<TDelegate, string>? InputInjector { get; set; }
    public Action<IFluentTemplateBuilder>? TemplateBuilderExtender { get; set; }

    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        if (InputInjector == null)
        {
            throw new InvalidOperationException($"Initialization incomplete. {nameof(InputInjector)} must be set.");
        }
        
        FileProvider ??= builder.GetFileProvider();
        return new JsonTemplateConfigurationProvider<TDelegate>(this, InputInjector, TemplateBuilderExtender);
    }
}