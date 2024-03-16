using System.Text;
using De.Loooping.Templates.Core.Configuration;
using De.Loooping.Templates.Core.Template;
using Microsoft.Extensions.Configuration.Json;

namespace De.Loooping.Templates.Configuration;

public class JsonTemplateConfigurationProvider<TDelegate>: JsonConfigurationProvider
    where TDelegate : class, MulticastDelegate
{
    private readonly Func<TDelegate, string> _inputInjector;
    private readonly Action<IFluentTemplateBuilder>? _templateBuilderExtender;

    public JsonTemplateConfigurationProvider(JsonConfigurationSource source, Func<TDelegate, string> inputInjector,
        Action<IFluentTemplateBuilder>? templateBuilderExtender) : base(source)
    {
        _inputInjector = inputInjector;
        _templateBuilderExtender = templateBuilderExtender;
    }

    public override void Load(Stream input)
    {
        string content = StreamToString(input);
        string processedContent = ProcessTemplate(content);
        Stream output = StringToStream(processedContent);
        base.Load(output);
    }
        
    private static string StreamToString(Stream stream)
    {
        stream.Position = 0;
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }

    private static Stream StringToStream(string src)
    {
        byte[] byteArray = Encoding.UTF8.GetBytes(src);
        return new MemoryStream(byteArray);
    }
        
    private string ProcessTemplate(string text)
    {
        var builder = new TemplateBuilder<TDelegate>(text, new TemplateProcessorConfiguration()
        {
            LeftContentDelimiter = "/*{{",
            RightContentDelimiter = "}}*/",
            LeftStatementDelimiter = "/*{%",
            RightStatementDelimiter = "%}*/",
            LeftCommentDelimiter = "/*{#",
            RightCommentDelimiter = "#}*/"
        });
        
        _templateBuilderExtender?.Invoke(builder);
        var template = builder.Build();

        string json = _inputInjector(template);
        return json;
    }
}