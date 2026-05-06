using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Buffers;

namespace MeshWiz.RefLinq;

public static partial class Iterator
{
    public static T[] ToArray<T>(this IEnumerable<T> enumerable) =>
        enumerable switch
        {
            T[] arr => arr.AsSpan().ToArray(),
            System.Collections.Generic.List<T> l => CollectionsMarshal.AsSpan(l).ToArray(),
            ICollection<T> c => IColToArray(c),
            ICollection c2 => IColToArray<T>(c2),
            _ => ArrayBuilder.Helper<T>.ToArray(enumerable)
        };
    //
    // private static T[] KnownCountToArray<T>(IEnumerable<T> enumerable, int rCount)
    // {
    //     var arr = GC.AllocateUninitializedArray<T>(rCount);
    //     var pos = -1;
    //     foreach (var item in enumerable) arr[++pos] = item;
    //     return arr;
    // }

    // ReSharper disable once InconsistentNaming
    internal static T[] IColToArray<T>(ICollection<T> collection)
    {
        if (collection.Count == 0) return [];
        var arr = GC.AllocateUninitializedArray<T>(collection.Count);
        collection.CopyTo(arr, 0);
        return arr;
    }

    // ReSharper disable once InconsistentNaming
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static T[] IColToArray<T>(ICollection collection)
    {
        if (collection.Count == 0) return [];
        var arr = GC.AllocateUninitializedArray<T>(collection.Count);
        collection.CopyTo(arr, 0);
        return arr;
    }

    public static System.Collections.Generic.List<T> ToList<T>(this IEnumerable<T> enumerable) =>
        enumerable switch
        {
            T[] arr => arr.AsSpan().ToList(),
            System.Collections.Generic.List<T> l => CollectionsMarshal.AsSpan(l).ToList(),
            _ => ArrayBuilder.Helper<T>.ToList(enumerable)
        };

    internal static System.Collections.Generic.List<T> KnownCountToList<T>(IEnumerable<T> enumerable, int count)
    {
        System.Collections.Generic.List<T> l = new(count);
        CollectionsMarshal.SetCount(l, count);
        var lSpan = CollectionsMarshal.AsSpan(l);
        var pos = -1;
        foreach (var item in enumerable) lSpan[++pos] = item;
        return l;
    }



    public static System.Collections.Generic.List<T> ToList<T>(this ReadOnlySpan<T> span)
    {
        System.Collections.Generic.List<T> l = new(span.Length);
        CollectionsMarshal.SetCount(l, span.Length);
        span.CopyTo(CollectionsMarshal.AsSpan(l));
        return l;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TItem[] ToArray<TIter, TItem>(TIter iter)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
    {
        // ReSharper disable once InvertIf - fast path
        using var iterCopy=iter.GetEnumerator();
        if (iterCopy.TryGetNonEnumeratedCount(out var fastCount))
        {
            if (fastCount == 0) return [];
            var array = GC.AllocateUninitializedArray<TItem>(fastCount);
            iterCopy.CopyTo(array);
            return array;
        }

        return ArrayBuilder.Helper<TItem>.ToArray(iterCopy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static System.Collections.Generic.List<TItem> ToList<TIter, TItem>(TIter iter)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
    {
        // ReSharper disable once InvertIf - fast path
        using var iterCopy=iter.GetEnumerator();
        if (iterCopy.TryGetNonEnumeratedCount(out var fastCount))
        {
            if (fastCount == 0)
                return [];
            var l = new System.Collections.Generic.List<TItem>(fastCount);
            CollectionsMarshal.SetCount(l, fastCount);
            iterCopy.CopyTo(CollectionsMarshal.AsSpan(l));
            return l;
        }

        return ArrayBuilder.Helper<TItem>.ToList(iterCopy);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? GetOrDefault<T>(this ReadOnlySpan<T> span, int index)
        => span.Length > (uint)index ? span[index] : default;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SpanIterator<T> Iterate<T>(this ReadOnlySpan<T> span) => span;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WhereIterator<SpanIterator<T>, T> Where<T>(this Span<T> span, Func<T, bool> predicate)
        => new(span, predicate);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<T> AsSpan<TInline, T>(this ref TInline array)
        where TInline : struct => array.AsSpan<TInline, T>(Unsafe.SizeOf<TInline>() / Unsafe.SizeOf<T>());
}