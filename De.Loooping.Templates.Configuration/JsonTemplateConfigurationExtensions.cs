using De.Loooping.Templates.Core.Template;
using Microsoft.Extensions.Configuration;

namespace De.Loooping.Templates.Configuration;

public static class JsonTemplateConfigurationExtensions
{
    public static IConfigurationBuilder AddJsonTemplateFile(
        this IConfigurationBuilder builder, string path,
        bool optional = false, bool reloadOnChange = false)
    {
        Func<Process,string> inputInjector = process => process();
        return AddJsonTemplateFile<Process>(builder, path, inputInjector, optional, reloadOnChange);
    }

    public static IConfigurationBuilder AddJsonTemplateFile<TDelegate>(this IConfigurationBuilder builder, string path,
        Func<TDelegate, string> inputInjector, bool optional = false, bool reloadOnChange = false, Action<IFluentTemplateBuilder>? build = null)
        where TDelegate : class, MulticastDelegate
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException($"{nameof(path)} must not be null", nameof(path));
        }

        return builder.Add((Action<JsonTemplateConfigurationSource<TDelegate>>)(s =>
        {
            s.Path = path;
            s.Optional = optional;
            s.ReloadOnChange = reloadOnChange;
            s.ResolveFileProvider();
            s.InputInjector = inputInjector;
            s.TemplateBuilderExtender = build;
        }));
    }
}