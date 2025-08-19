using System.Diagnostics.Contracts;
using System.Numerics;

namespace MeshWiz.Utility.Extensions;

public static class EnumerableExt
{
    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (var item in enumerable) action(item);
    }

    [Pure]
    public static TAdd Sum<TAdd>(this IEnumerable<TAdd> enumerable)
        where TAdd : struct, IAdditionOperators<TAdd, TAdd, TAdd>
    {
        var sum = default(TAdd);
        return enumerable.Aggregate(sum, (current, item) => current + item);
    }

    [Pure]
    public static TAdd Sum<TSource, TAdd>(this IEnumerable<TSource> enumerable, Func<TSource, TAdd> selector)
        where TAdd : struct, IAdditionOperators<TAdd, TAdd, TAdd>
        => enumerable.Aggregate(default(TAdd), (accum, item) => accum + selector(item));
}