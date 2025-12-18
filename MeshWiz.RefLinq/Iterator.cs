using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.RefLinq;

public static class Iterator
{
    internal static TItem First<TIter, TItem>(TIter source)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
    {
        return source.TryGetFirst(out var first) ? first! : ThrowHelper.ThrowInvalidOperationException<TItem>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetFirst<TIter, TItem>(TIter source, out TItem? item)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
    {
        var found = source.MoveNext();
        item = found ? source.Current : default;
        return found;
    }

    internal static bool TryGetLast<TIter, TItem>(TIter source, out TItem? last)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
    {
        var found = false;
        last = default;
        while (source.MoveNext())
        {
            found = true;
            last = source.Current;
        }

        return found;
    }

    internal static TItem Last<TIter, TItem>(TIter source)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct =>
        source.TryGetLast(out var last)
            ? last!
            : ThrowHelper.ThrowInvalidOperationException<TItem>();

    internal static int Count<TIter, TIgnore>(TIter iter)
        where TIter : IRefIterator<TIter, TIgnore>, allows ref struct
    {
        if (iter.TryGetNonEnumeratedCount(out var count))
            return count;
        var c = 0;
        while (iter.MoveNext())
            c++;
        return c;
    }


    internal static void CopyTo<TIter, TItem>(TIter iter, Span<TItem> destination)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
    {
        if (iter.TryConvertToSpanIter<TIter, TItem>(out var spanIterator))
        {
            spanIterator.OriginalSource.CopyTo(destination);
            return;
        }

        var i = -1;
        while (iter.MoveNext()) destination[++i] = iter.Current;
    }

    public static bool TryConvertToSpanIter<TIter, TItem>(this TIter iter, out SpanIterator<TItem> span)
        where TIter : allows ref struct
    {
        span = default;

        var correctType = typeof(TIter) == typeof(SpanIterator<TItem>);
        span = correctType ? Unsafe.As<TIter, SpanIterator<TItem>>(ref Unsafe.AsRef(in iter)) : default;
        return correctType;
    }

    internal static TItem[] ToArray<TIter, TItem>(TIter iter)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
    {
        TItem[] array;
        if (iter.TryGetNonEnumeratedCount(out var count))
        {
            array = new TItem[count];
            iter.CopyTo(array);
            return array;
        }

        var i = 0;
        array = new TItem[iter.MaxPossibleCount()];
        while (iter.MoveNext())
            array[i++]=iter.Current;
        Array.Resize(ref array, i);
        return array;
    }

    private static void UnsafeAdd<T>(ref T[] arr, T item, ref int count)
    {
        if (arr.Length == count) Array.Resize(ref arr, count * 2);
        arr[count] = item;
        count++;
    }

    internal static List<TItem> ToList<TIter, TItem>(TIter iter)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
    {
        List<TItem> l = iter.TryGetNonEnumeratedCount(out var capa) ? new(capa) : new();
        while (iter.MoveNext()) l.Add(iter.Current);
        return l;
    }

    internal static HashSet<TItem> ToHashSet<TIter, TItem>(TIter iter, IEqualityComparer<TItem>? comp = null)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
    {
        var l = iter.TryGetNonEnumeratedCount(out var capa)
            ? new HashSet<TItem>(capa, comp)
            : new HashSet<TItem>(comp);
        while (iter.MoveNext()) l.Add(iter.Current);
        return l;
    }

    internal static RangeIterator<TSource, TItem> Take<TSource, TItem>(TSource source, int num)
        where TSource : IRefIterator<TSource, TItem>, allows ref struct =>
        new(source, ..num);

    internal static RangeIterator<TSource, TItem> Take<TSource, TItem>(TSource source, Range range)
        where TSource : IRefIterator<TSource, TItem>, allows ref struct =>
        new(source, range);

    internal static RangeIterator<TSource, TItem> Skip<TSource, TItem>(TSource source, int skip)
        where TSource : IRefIterator<TSource, TItem>, allows ref struct =>
        new(source, skip..);

    public static Dictionary<TKey, TValue> ToDictionary<TIter, TItem, TKey, TValue>(
        TIter source,
        IEqualityComparer<TKey> keyComparer,
        Func<TItem, TKey> keySelector,
        Func<TItem, TValue> valueSelector
    )
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
        where TKey : notnull
    {
        source.Reset();
        var dict = source.TryGetNonEnumeratedCount(out var c)
            ? new Dictionary<TKey, TValue>(c, keyComparer)
            : new Dictionary<TKey, TValue>(keyComparer);
        while (source.MoveNext()) dict[keySelector(source.Current)] = valueSelector(source.Current);
        return dict;
    }

    public static Dictionary<TKey, TItem> ToDictionary<TIter, TItem, TKey>(
        TIter source,
        IEqualityComparer<TKey> keyComparer,
        Func<TItem, TKey> keySelector) where TIter : IRefIterator<TIter, TItem> where TKey : notnull =>
        ToDictionary(source, keyComparer, keySelector, x => x);

}