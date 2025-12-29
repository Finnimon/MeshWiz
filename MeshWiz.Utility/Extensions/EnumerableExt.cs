using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;

namespace MeshWiz.Utility.Extensions;

public static class EnumerableExt
{
    public static IEnumerable<T> TakeAtMost<T>(this IEnumerable<T> enumerable, int count) 
        => enumerable.TakeWhile((_, i) => i <= count);

    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (var item in enumerable) action(item);
    }
    
    public static void ForEach<TArg,TIgnore>(this IEnumerable<TArg> enumerable, Func<TArg,TIgnore> func)
    {
        foreach (var item in enumerable) func(item);
    }

    [Pure]
    public static TAdd Sum<TAdd>(this IEnumerable<TAdd> enumerable,TAdd zero)
        where TAdd :IAdditionOperators<TAdd, TAdd, TAdd> 
    {
        var sum = zero;
        return enumerable.Aggregate(sum, (current, item) => current + item);
    }

    
    [Pure]
    [SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
    public static TAdd Sum<TAdd>(this IEnumerable<TAdd> enumerable)
        where TAdd : INumberBase<TAdd> =>
        enumerable switch
        {
            IEnumerable<float> nums => (TAdd)(object)Enumerable.Average(nums),
            _ => enumerable.Aggregate(TAdd.AdditiveIdentity, (current, item) => current + item)
        };

    [Pure]
    public static TAdd Sum<TSource, TAdd>(this IEnumerable<TSource> enumerable, Func<TSource, TAdd> selector)
        where TAdd : unmanaged, IAdditionOperators<TAdd, TAdd, TAdd>
        => enumerable.Aggregate(default(TAdd), (accum, item) => accum + selector(item));
}