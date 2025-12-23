using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace MeshWiz.RefLinq;

public static class SpanExt
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? GetOrDefault<T>(this ReadOnlySpan<T> span, int index)
        => span.Length > (uint)index ? span[index] : default;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? GetOrDefault<T>(this ReadOnlySpan<T> span, Index index)
    {
        var idx = index.IsFromEnd ? span.Length - index.Value : index.Value;
        return span.GetOrDefault(idx);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGet<T>(this ReadOnlySpan<T> span, int index, out T? value)
    {
        if (span.Length > (uint)index)
        {
            value = span[index];
            return true;
        }

        value = default;
        return false;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGet<T>(this ReadOnlySpan<T> span, Index index, out T? value)
    {
        var idx = index.IsFromEnd ? span.Length - index.Value : index.Value;
        return span.TryGet(idx, out value);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SpanIterator<T> Iterate<T>(this ReadOnlySpan<T> span) => span;
    
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SmartSelectIterator<TIn,TOut> Select<TIn,TOut>(this ReadOnlySpan<TIn> span,Func<TIn,TOut> sel)
    => new(span, sel);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WhereIterator<SpanIterator<T>,T> Where<T>(this ReadOnlySpan<T> span,Func<T,bool> predicate)
        => new(span, predicate);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OfTypeIterator<SpanIterator<TIn>, TIn, TOut> OfType<TIn, TOut>(this ReadOnlySpan<TIn> sp)
        => new(sp);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe Span<T> AsSpan<TInline, T>(this ref TInline array,int size)
        where TInline : struct =>
        new(Unsafe.AsPointer(ref array), size);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<T> AsSpan<TInline, T>(this ref TInline array)
        where TInline : struct =>array.AsSpan<TInline,T>(Unsafe.SizeOf<TInline>()/Unsafe.SizeOf<T>());
}