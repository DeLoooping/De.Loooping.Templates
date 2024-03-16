using System.Reflection;

namespace De.Loooping.Templates.Core.Template;

public interface ITemplateBuilder
{
    void AddType<T>();
    void AddType(Type type);
    void AddUsing(string @using);
    void AddReference(Assembly reference);
}