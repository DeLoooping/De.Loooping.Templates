using De.Loooping.Templates.Core.Tools;

namespace De.Loooping.Templates.Tests.Tools;

public class TypeNameResolverTests
{
    public class PublicNestedClass { }
    private class PrivateNestedClass { }
    
    [Theory(DisplayName = $"{nameof(TypeNameResolver)} resolves correct full names including namespace")]
    [InlineData(typeof(string), "System.String")]
    [InlineData(typeof(int), "System.Int32")]
    [InlineData(typeof(TypeNameResolverTests), "De.Loooping.Templates.Tests.Tools.TypeNameResolverTests")]
    [InlineData(typeof(PublicNestedClass), "De.Loooping.Templates.Tests.Tools.TypeNameResolverTests.PublicNestedClass")]
    [InlineData(typeof(PrivateNestedClass), "De.Loooping.Templates.Tests.Tools.TypeNameResolverTests.PrivateNestedClass")]
    [InlineData(typeof(IEnumerable<string>), "System.Collections.Generic.IEnumerable<System.String>")]
    [InlineData(typeof(IEnumerable<List<string>>), "System.Collections.Generic.IEnumerable<System.Collections.Generic.List<System.String>>")]
    [InlineData(typeof(KeyValuePair<string, int>), "System.Collections.Generic.KeyValuePair<System.String, System.Int32>")]
    public void TypeNameResolverResolves(Type type, string expectedFullName)
    {
        string fullName = TypeNameResolver.GetFullName(type);
        Assert.Equal(expectedFullName, fullName);
    }
}