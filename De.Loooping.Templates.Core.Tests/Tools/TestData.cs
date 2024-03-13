using System.Collections;
using System.Runtime.CompilerServices;

namespace De.Loooping.Templates.Core.Tests.Tools;

public abstract class TestData<T> : IEnumerable<object?[]>
    where T: ITuple
{
    public IEnumerator<object?[]> GetEnumerator()
    {
        foreach (T tuple in GetData())
        {
            yield return Deconstruct(tuple).ToArray();
        }
    }

    private IEnumerable<object?> Deconstruct(T tuple)
    {
        for (int i = 0; i < tuple.Length; i++)
        {
            yield return tuple[i];
        }
    }

    protected abstract IEnumerable<T> GetData();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}