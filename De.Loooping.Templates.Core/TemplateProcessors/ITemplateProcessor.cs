namespace De.Loooping.Templates.Core.TemplateProcessors;

public interface ITemplateProcessor
{
    string Process(string template);
}

public interface ITemplateProcessor<in T>
{
    string Process(string template, T replacementValues);
}