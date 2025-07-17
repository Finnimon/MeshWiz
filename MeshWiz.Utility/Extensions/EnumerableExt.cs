using System.Diagnostics.Contracts;

namespace MeshWiz.Utility.Extensions;

public static class EnumerableExt
{
    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (var item in enumerable) action(item);
    }
}