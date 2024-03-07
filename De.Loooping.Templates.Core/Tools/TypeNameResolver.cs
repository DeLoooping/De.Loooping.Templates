using System.Text;
using System.Text.RegularExpressions;

namespace De.Loooping.Templates.Core.Tools;

internal static class TypeNameResolver
{
    public static string GetFullName(Type type)
    {
        if (type.IsGenericType)
        {
            string baseName = type.FullName!.Split('`', 2)[0];
            IEnumerable<string> typeArgumentNames = type.GenericTypeArguments.Select(GetFullName);
            return $"{baseName}<{String.Join(", ", typeArgumentNames)}>";
        }
        
        return type.FullName!.Replace("+", ".");
    }
}