using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using CommunityToolkit.Diagnostics;
using JetBrains.Annotations;
using MeshWiz.Utility;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;
using Range = System.Range;

namespace MeshWiz.RefLinq;

public static partial class Iterator
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TItem First<TIter, TItem>(TIter source)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
    {
        return source.TryGetFirst(out var first) ? first! : EnumerableThrowHelper.NoElements<TItem>();
    }


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

    [System.Diagnostics.Contracts.Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SequenceIterator<T> Sequence<T>(T start, T endInclusive, T step) where T : struct, INumber<T> =>
        new(start, endInclusive, step);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TItem Last<TIter, TItem>(TIter source)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct =>
        source.TryGetLast(out var last)
            ? last!
            : EnumerableThrowHelper.NoElements<TItem>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Count<TIter, TIgnore>(TIter iter)
        where TIter : IRefIterator<TIter, TIgnore>, allows ref struct
    {
        if (iter.TryGetNonEnumeratedCount(out var count))
            return count;
        using var mut = iter.GetEnumerator();
        var c = 0;
        while (mut.MoveNext())
            c++;
        return c;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void CopyTo<TIter, TItem>(TIter iter, System.Span<TItem> destination)
        where TIter : IEnumerator<TItem>, allows ref struct
    {
        var i = -1;
        using var iterCopy = iter;
        while (iterCopy.MoveNext()) destination[++i] = iterCopy.Current;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryConvertToSpanIter<TIter, TItem>(this TIter iter, out SpanIterator<TItem> span)
        where TIter : allows ref struct
    {
        span = default;

        var correctType = typeof(TIter) == typeof(SpanIterator<TItem>);
        span = correctType ? Unsafe.As<TIter, SpanIterator<TItem>>(ref iter) : default;
        return correctType;
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

    internal static RangedIterator<TSource, TItem> Take<TSource, TItem>(TSource source, int num)
        where TSource : IRefIterator<TSource, TItem>, allows ref struct =>
        new(source, ..num);

    internal static RangedIterator<TSource, TItem> Take<TSource, TItem>(TSource source, Range range)
        where TSource : IRefIterator<TSource, TItem>, allows ref struct =>
        new(source, range);

    internal static RangedIterator<TSource, TItem> Skip<TSource, TItem>(TSource source, int skip)
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
        where TNum : struct, INumber<TNum>
    {
        if (iter.TryConvertToSpanIter<TIterator, TNum>(out var spanIter))
            return spanIter.OriginalSource.Sum();
        iter.Reset();
        while (iter.MoveNext()) seed += iter.Current;
        return seed;
    }
    #nullable disable
    public static TNum Sum<TNum, TIn>(this SmartSelectIterator<TIn, TNum> iter, TNum seed)
        where TNum : struct, INumber<TNum>
    {
        if (iter.Source.IsEmpty) return seed;
        ref var position = ref MemoryMarshal.GetReference(iter.Source);
        ref var end = ref Unsafe.Add(ref position, iter.Source.Length);
        var sum = seed;
        if (Vector128.IsHardwareAccelerated && Vector128<TNum>.IsSupported &&
            iter.Source.Length >= Vector128<TNum>.Count)
        {
            ref var vectorizedEnd = ref Unsafe.Subtract(ref end, Vector128<TNum>.Count);
            var res = Vector128<TNum>.Zero;
            var adder = res;
            ref var adderPin = ref Unsafe.As<Vector128<TNum>, TNum>(ref adder);
            while (Unsafe.IsAddressLessThanOrEqualTo(ref position, ref vectorizedEnd))
            {
                for (var i = 0; i < Vector128<TNum>.Count; i++)
                    Unsafe.Add(ref adderPin, i) = iter._sel(Unsafe.Add(ref position, i));
                res += adder;
                position = ref Unsafe.Add(ref position, Vector<TNum>.Count);
            }

            sum = Vector128.Sum(res);
        }

        for (; Unsafe.IsAddressLessThan(ref position, ref end); position = ref Unsafe.Add(ref position, 1))
            sum += iter._sel(position);
        return sum;
    }
    #nullable enable
    public static ConcatIterator<SpanIterator<T>, TOther, T> Concat<T, TOther>(this ReadOnlySpan<T> span,
        TOther other)
        where TOther : IRefIterator<TOther, T>, allows ref struct
        => new(span, other);

    public static TNum Sum<TNum>(this System.ReadOnlySpan<TNum> nums)
        where TNum : struct, INumber<TNum>
    {
        ref var position = ref MemoryMarshal.GetReference(nums);
        ref var local2 = ref Unsafe.Add(ref position, nums.Length);
        var sum = TNum.Zero;
        if (Vector128.IsHardwareAccelerated && Vector128<TNum>.IsSupported && nums.Length >= Vector128<TNum>.Count)
        {
            var res = Vector128.LoadUnsafe(ref position);
            position = ref Unsafe.Add(ref position, Vector128<TNum>.Count);
            ref var local3 = ref Unsafe.Subtract(ref local2, Vector128<TNum>.Count);
            do
            {
                res += Vector128.LoadUnsafe(ref position);
                position = ref Unsafe.Add(ref position, Vector<TNum>.Count);
            } while (Unsafe.IsAddressLessThanOrEqualTo(ref position, ref local3));

            sum = Vector128.Sum(res);
        }

        for (; Unsafe.IsAddressLessThan(ref position, ref local2); position = ref Unsafe.Add(ref position, 1))
            sum += position;
        return sum;
    }

    public static System.Span<TItem> Take<TItem>(this List<TItem> data, Range range) =>
        CollectionsMarshal.AsSpan(data)[range];

    public static System.Span<TItem> Take<TItem>(this List<TItem> data, int num) =>
        CollectionsMarshal.AsSpan(data)[..num];

    public static System.Span<TItem> Skip<TItem>(this List<TItem> data, int num) =>
        CollectionsMarshal.AsSpan(data)[num..];

    public static System.Span<TItem> Take<TItem>(this TItem[] data, Range range) => data.AsSpan(range);
    public static System.Span<TItem> Take<TItem>(this TItem[] data, int num) => data.AsSpan(0, num);
    public static System.Span<TItem> Skip<TItem>(this TItem[] data, int num) => data.AsSpan(num);

    internal static TItem Aggregate<TIter, TItem>(TIter iter, Func<TItem, TItem, TItem> aggregator)
        where TIter : IRefIterator<TIter, TItem>, allows ref struct
    {
        if (!iter.MoveNext())
            EnumerableThrowHelper.NoElements();
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Iterator<T> Iterate<T>(this IEnumerable<T> source) => new(source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SpanIterator<T> Iterate<T>(this List<T> source) => source;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo<T>(this IEnumerable<T> source, System.Span<T> destination) =>
        source.Iterate().CopyTo(destination);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo<T>(this IEnumerable<T> source, T[] destination, int index) =>
        source.Iterate().CopyTo(destination.AsSpan(index));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetSpan<T>([NoEnumeration] this IEnumerable<T> enumerable, out ReadOnlySpan<T> data)
    {
        switch (enumerable)
        {
            case T[] arr:
                data = arr;
                return true;
            case List<T> l:
                data = CollectionsMarshal.AsSpan(l);
                return true;
            default:
                data = ReadOnlySpan<T>.Empty;
                return enumerable
                    is ICollection<T> { Count: 0 }
                    or ICollection { Count: 0 }
                    or IReadOnlyCollection<T> { Count: 0 };
        }
    }

    public static bool Any<TIter, T>(this TIter iter, Func<T, bool> test)
        where TIter : IRefIterator<TIter, T>
    {
        if (iter.TryGetNonEnumeratedCount(out var c) && c == 0) return false;
        using var copy = iter.GetEnumerator();
        while (copy.MoveNext())
        {
            if (!test(copy.Current)) continue;
            return true;
        }

        return false;
    }

    public static bool TryGetNonEnumeratedCount<T>([NoEnumeration] this IEnumerable<T> enumerable, out int count)
    {
        count = enumerable switch
        {
            ICollection<T> coll => coll.Count,
            IReadOnlyCollection<T> coll => coll.Count,
            ICollection coll => coll.Count,
            _ => -1
        };

        return count != -1;
    }

    public static RangeIterator<T> Range<T>(T start, int count)
        where T : struct, INumber<T>
        => new(start, count);

    [System.Diagnostics.Contracts.Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGet<T>(this ReadOnlySpan<T> span, Index index, out T? value)
    {
        var idx = index.IsFromEnd ? span.Length - index.Value : index.Value;
        return span.TryGet(idx, out value);
    }

    [System.Diagnostics.Contracts.Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WhereIterator<SpanIterator<T>, T> Where<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate)
        => new(span, predicate);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Swap<T>(this Span<T> span, int left, int right)
        => (span[left], span[right]) = (span[right], span[left]);
}