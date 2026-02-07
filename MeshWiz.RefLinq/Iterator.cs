using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using JetBrains.Annotations;
using MeshWiz.Utility;

namespace MeshWiz.RefLinq;

public static partial class Iterator
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TItem First<TIter, TItem>(TIter source)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
    {
        return source.TryGetFirst(out var first) ? first! : ThrowHelper.ThrowInvalidOperationException<TItem>();
    }

    public static AdapterIterator<T> Iterate<T>(this IEnumerable<T> c) => new AdapterIterator<T>(c);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetFirst<TIter, TItem>(TIter source, out TItem? item)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
    {
        var found = source.MoveNext();
        item = found ? source.Current : default;
        return found;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetLast<TIter, TItem>(TIter source, out TItem? last)
        where TIter : IEnumerator<TItem>, allows ref struct
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TItem Last<TIter, TItem>(TIter source)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct =>
        source.TryGetLast(out var last)
            ? last!
            : ThrowHelper.ThrowInvalidOperationException<TItem>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryConvertToSpanIter<TIter, TItem>(this TIter iter, out SpanIterator<TItem> span)
        where TIter : allows ref struct
    {
        span = default;

        var correctType = typeof(TIter) == typeof(SpanIterator<TItem>);
        span = correctType ? Unsafe.As<TIter, SpanIterator<TItem>>(ref Unsafe.AsRef(in iter)) : default;
        return correctType;
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

        
        using BufferedArrayBuilder<TItem> builder = new(int.Max(8,iter.EstimateCount()));
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
            CollectionsMarshal.SetCount(l,fastCount);
            iter.CopyTo(CollectionsMarshal.AsSpan(l));
            return l;
        }
        using BufferedArrayBuilder<TItem> builder = new(int.Max(8,iter.EstimateCount()));
        builder.AddEnumeratorInlined(iter);
        return builder.ToList();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int PreSize<TIter, TItem>(TIter iter) where TIter : IRefIterator<TIter, TItem>, allows ref struct
    {
        var preSize = iter.TryGetNonEnumeratedCount(out var capa) ? capa : iter.EstimateCount();
        return preSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static HashSet<TItem> ToHashSet<TIter, TItem>(TIter iter, IEqualityComparer<TItem>? comp = null)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
    {
        HashSet<TItem> l = new(PreSize<TIter, TItem>(iter), comp);
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
        IEqualityComparer<TKey>? keyComparer,
        Func<TItem, TKey> keySelector,
        Func<TItem, TValue> valueSelector
    )
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
        where TKey : notnull
    {
        source.Reset();
        var dict = new Dictionary<TKey, TValue>(PreSize<TIter, TItem>(source), keyComparer);
        while (source.MoveNext()) dict[keySelector(source.Current)] = valueSelector(source.Current);
        return dict;
    }

    public static TNum Sum<TIterator, TNum>(this TIterator iter, TNum seed)
        where TIterator : IEnumerator<TNum>, allows ref struct
        where TNum : struct, IAdditionOperators<TNum, TNum, TNum>
    {
        iter.Reset();
        while (iter.MoveNext()) seed += iter.Current;
        return seed;
    }

    public static ReadOnlySpan<TItem> Take<TItem>(this ReadOnlySpan<TItem> span, Range range) => span[range];
    public static ReadOnlySpan<TItem> Take<TItem>(this ReadOnlySpan<TItem> span, int num) => span[..num];
    public static ReadOnlySpan<TItem> Skip<TItem>(this ReadOnlySpan<TItem> span, int num) => span[num..];

    public static Span<TItem> TakeSpan<TItem>(this List<TItem> data, Range range) =>
        CollectionsMarshal.AsSpan(data)[range];

    public static Span<TItem> TakeSpan<TItem>(this List<TItem> data, int num) => CollectionsMarshal.AsSpan(data)[..num];
    public static Span<TItem> SkipSpan<TItem>(this List<TItem> data, int num) => CollectionsMarshal.AsSpan(data)[num..];

    public static Span<TItem> TakeSpan<TItem>(this TItem[] data, Range range) => (data).AsSpan(range);
    public static Span<TItem> TakeSpan<TItem>(this TItem[] data, int num) => (data).AsSpan(0, num);
    public static Span<TItem> SkipSpan<TItem>(this TItem[] data, int num) => (data).AsSpan(num);

    internal static TItem Aggregate<TIter, TItem>(TIter iter, Func<TItem, TItem, TItem> aggregator)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
    {
        if (!iter.MoveNext())
            ThrowHelper.ThrowInvalidOperationException();
        var seed = iter.Current;
        while (iter.MoveNext())
            seed = aggregator(seed, iter.Current);
        return seed;
    }

    public static int Count<TIter, T>(this TIter iter, Func<T, bool> test) 
        where TIter : IRefIterator<TIter, T>, allows ref struct
        => iter.Where(test).Count();


    internal static TOut Aggregate<TIter, TItem, TOut>(TIter iter, Func<TOut, TItem, TOut> aggregator, TOut seed)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
    {
        while (iter.MoveNext())
            seed = aggregator(seed, iter.Current);
        return seed;
    }

    internal static AdapterIterator<T> Adapt<T>(this IEnumerable<T> source) => new(source);

    public static ItemIterator<T> Repeat<T>(T item, int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);
        return new ItemIterator<T>(item, count);
    }

    public static bool TryGetMin<TIter, TItem>(TIter iter, IComparer<TItem>? comp,
        [NotNullWhen(returnValue: true)] out TItem? min)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
    {
        var comparer = comp ?? Comparer<TItem>.Default;
        var first = Bool.Once();
        min = default;
        while (iter.MoveNext())
        {
            var cur = iter.Current;
            if (first || comparer.Compare(min, cur) > 0) min = cur;
        }

        return !first;
    }

    public static bool TryGetMax<TIter, TItem>(TIter iter, IComparer<TItem>? comp,
        [NotNullWhen(returnValue: true)] out TItem? min)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
    {
        iter.Reset();
        var comparer = comp ?? Comparer<TItem>.Default;
        var first = Bool.Once();
        min = default;
        while (iter.MoveNext())
        {
            var cur = iter.Current;
            if (first || comparer.Compare(min, cur) > 0) min = cur;
        }

        return !first;
    }

    
    public static bool TryGetSpan<T>([NoEnumeration] this IEnumerable<T> enumerable, out ReadOnlySpan<T> data)
    {
        if (enumerable is T[] arr)
        {
            data = arr;
            return true;
        }

        if (enumerable is List<T> l)
        {
            data = CollectionsMarshal.AsSpan(l);
            return true;
        }

        data = ReadOnlySpan<T>.Empty;
        var emptyCorrect=enumerable.TryGetNonEnumeratedCount(out var count) && count == 0;
        return emptyCorrect;
    }

    public static bool TryGetNonEnumeratedCount<T>([NoEnumeration] this IEnumerable<T> enumerable, out int count)
    {
        count = enumerable switch
        {
            ICollection<T> coll => coll.Count,
            IReadOnlyCollection<T> coll => coll.Count,
            _ => -1
        };

        return count!=-1;
    }
}