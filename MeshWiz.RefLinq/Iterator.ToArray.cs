using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Buffers;

namespace MeshWiz.RefLinq;

public static partial class Iterator
{
    public static T[] ToArray<T>(IEnumerable<T> enumerable) =>
        enumerable switch
        {
            T[] arr => arr.AsSpan().ToArray(),
            List<T> l => CollectionsMarshal.AsSpan(l).ToArray(),
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

    public static List<T> ToList<T>(this IEnumerable<T> enumerable) =>
        enumerable switch
        {
            T[] arr => arr.AsSpan().ToList(),
            List<T> l => CollectionsMarshal.AsSpan(l).ToList(),
            _ => ArrayBuilder.Helper<T>.ToList(enumerable)
        };

    internal static List<T> KnownCountToList<T>(IEnumerable<T> enumerable, int count)
    {
        List<T> l = new(count);
        CollectionsMarshal.SetCount(l, count);
        var lSpan = CollectionsMarshal.AsSpan(l);
        var pos = -1;
        foreach (var item in enumerable) lSpan[++pos] = item;
        return l;
    }



    public static List<T> ToList<T>(this ReadOnlySpan<T> span)
    {
        List<T> l = new(span.Length);
        CollectionsMarshal.SetCount(l, span.Length);
        span.CopyTo(CollectionsMarshal.AsSpan(l));
        return l;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TItem[] ToArray<TIter, TItem>(TIter iter)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
    {
        if (iter.TryGetNonEnumeratedCount(out var fastCount))
        {
            if (fastCount == 0) return [];
            var array = GC.AllocateUninitializedArray<TItem>(fastCount);
            iter.CopyTo(array);
            return array;
        }

        return ArrayBuilder.Helper<TItem>.ToArray(iter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static List<TItem> ToList<TIter, TItem>(TIter iter)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
    {
        if (iter.TryGetNonEnumeratedCount(out var fastCount))
        {
            if (fastCount == 0)
                return [];
            var l = new List<TItem>(fastCount);
            CollectionsMarshal.SetCount(l, fastCount);
            iter.CopyTo(CollectionsMarshal.AsSpan(l));
            return l;
        }

        return ArrayBuilder.Helper<TItem>.ToList(iter);
    }
}