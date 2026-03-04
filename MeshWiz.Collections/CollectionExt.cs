using System.Runtime.CompilerServices;

namespace MeshWiz.Collections;

public static class CollectionExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SelectList<TIn, TOut> SelectList<TIn, TOut>(this IReadOnlyList<TIn> source, Func<TIn, TOut> selector) 
        => new(source, selector);
}