using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MeshWiz.RefLinq;

public static partial class Iterator
{
    public static T[] ToArray<T>(IEnumerable<T> enumerable) =>
        enumerable switch
        {
            T[] arr => arr.AsSpan().ToArray(),
            List<T> l => CollectionsMarshal.AsSpan(l).ToArray(),
            ICollection<T> c => IColToArray(c),
            IReadOnlyCollection<T> r => KnownCountToArray(r, r.Count),
            ICollection c2 => IColToArray<T>(c2),
            _ => ArrBuilderToArray(enumerable)
        };
    
    private static T[] ArrBuilderToArray<T>(IEnumerable<T> enumerable)
    {
        using BufferedArrayBuilder<T> b = new();
        b.AddEnumeratingInlined(enumerable);
        return b.ToArray();
    }

    private static T[] KnownCountToArray<T>(IEnumerable<T> enumerable, int rCount)
    {
        var arr = GC.AllocateUninitializedArray<T>(rCount);
        var pos = -1;
        foreach (var item in enumerable) arr[++pos] = item;
        return arr;
    }

    // ReSharper disable once InconsistentNaming
    private static T[] IColToArray<T>(ICollection<T> collection)
    {
        var arr = GC.AllocateUninitializedArray<T>(collection.Count);
        collection.CopyTo(arr, 0);
        return arr;
    }

    // ReSharper disable once InconsistentNaming
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T[] IColToArray<T>(ICollection collection)
    {
        var arr = GC.AllocateUninitializedArray<T>(collection.Count);
        collection.CopyTo(arr, 0);
        return arr;
    }

    public static List<T> ToList<T>(IEnumerable<T> enumerable) =>
        enumerable switch
        {
            T[] arr => arr.AsSpan().ToList(),
            List<T> l => CollectionsMarshal.AsSpan(l).ToList(),
            ICollection<T> c => KnownCountToList(c, c.Count),
            IReadOnlyCollection<T> r => KnownCountToList(r, r.Count),
            ICollection c2 => KnownCountToList(enumerable, c2.Count),
            _ => ArrBuilderToList(enumerable)
        };

    private static List<T> KnownCountToList<T>(IEnumerable<T> enumerable, int count)
    {
        List<T> l = new(count);
        CollectionsMarshal.SetCount(l, count);
        var lSpan = CollectionsMarshal.AsSpan(l);
        var pos = -1;
        foreach (var item in enumerable) lSpan[++pos] = item;
        return l;
    }


    private static List<T> ArrBuilderToList<T>(IEnumerable<T> enumerable)
    {
        using BufferedArrayBuilder<T> b = new();
        b.AddEnumeratingInlined(enumerable);
        return b.ToList();
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
            if (fastCount == 0)
                return [];
            var array = GC.AllocateUninitializedArray<TItem>(fastCount);
            iter.CopyTo(array);
            return array;
        }


        using BufferedArrayBuilder<TItem> builder = new(int.Max(8, iter.EstimateCount()));
        builder.AddEnumeratorInlined(iter);
        return builder.ToArray();
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

        using BufferedArrayBuilder<TItem> builder = new(int.Max(8, iter.EstimateCount()));
        builder.AddEnumeratorInlined(iter);
        return builder.ToList();
    }
}