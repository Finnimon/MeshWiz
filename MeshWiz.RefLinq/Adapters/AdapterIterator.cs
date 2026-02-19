using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Buffers;
using MeshWiz.Utility;

namespace MeshWiz.RefLinq;

public readonly partial struct Iterator<T> : IRefIterator<Iterator<T>, T>, IEnumerable<T>
{
    internal readonly Imp.IImp _imp;
    public Iterator(IEnumerable<T> source) => _imp = Imp.Create(source);
    internal Iterator(Imp.IImp imp) => _imp = imp;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => _imp.MoveNext();

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset() => _imp.Reset();

    /// <inheritdoc />
    public T Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _imp.Current;
    }

    /// <inheritdoc />
    object? IEnumerator.Current => _imp.Current;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => _imp.Dispose();

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T First() => Iterator.First<Iterator<T>, T>(this);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? FirstOrDefault()
    {
        TryGetFirst(out var first);
        return first;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetFirst(out T? item)
        => _imp.TryGetFirst(out item);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Last()
        => TryGetLast(out var last) ? last! : ThrowHelper.ThrowInvalidOperationException<T>();

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? LastOrDefault()
    {
        TryGetLast(out var last);
        return last;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetLast(out T? item) => _imp.TryGetLast(out item);

    /// <inheritdoc />
    public int Count()
        => _imp.Count();

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetNonEnumeratedCount(out int count) => _imp.TryGetNonEnumeratedCount(out count);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(Span<T> destination) => _imp.CopyTo(destination);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(T[] array, int arrayIndex) => CopyTo(array.AsSpan(arrayIndex));


    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Iterator<T> GetEnumerator()
    {
        var copy = this;
        copy.Reset();
        return copy;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WhereIterator<Iterator<T>, T> Where(Func<T, bool> predicate)
        => new(this, predicate);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SelectIterator<Iterator<T>, T, TOut> Select<TOut>(Func<T, TOut> selector)
        => new(this, selector);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RangedIterator<Iterator<T>, T> Take(Range r)
        => new(this, r);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RangedIterator<Iterator<T>, T> Take(int num)
        => Take(..num);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RangedIterator<Iterator<T>, T> Skip(int num)
        => Take(num..);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[] ToArray()
    {
        if (_imp.TryGetNonEnumeratedCount(out var count))
        {
            var arr = GC.AllocateUninitializedArray<T>(count);
            _imp.CopyTo(arr);
            return arr;
        }

        return ArrayBuilder.Helper<T>.ToArray(_imp.Underlying);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetSpan(out ReadOnlySpan<T> span) => _imp.TryGetSpan(out span);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<T> ToList()
    {
        if (_imp.TryGetNonEnumeratedCount(out var count))
        {
            List<T> list = new(count);
            CollectionsMarshal.SetCount(list, count);
            _imp.CopyTo(CollectionsMarshal.AsSpan(list));
            return list;
        }

        return ArrayBuilder.Helper<T>.ToList(_imp.Underlying);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HashSet<T> ToHashSet()
        => ToHashSet(null);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HashSet<T> ToHashSet(IEqualityComparer<T>? comp)
        => Iterator.ToHashSet(this, comp);

    /// <inheritdoc />
    public bool Any() => _imp.Any();

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int EstimateCount() => TryGetNonEnumeratedCount(out var count) ? count : 8;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OfTypeIterator<Iterator<T>, T, TOther> OfType<TOther>()
        => new(this);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Aggregate(Func<T, T, T> aggregator)
        => Iterator.Aggregate(this, aggregator);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TOther Aggregate<TOther>(Func<TOther, T, TOther> aggregator, TOther seed)
        => Iterator.Aggregate(this, aggregator, seed);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(Func<T, TKey> keyGen, Func<T, TValue> valGen,
        IEqualityComparer<TKey>? comp) where TKey : notnull
        => Iterator.ToDictionary(this, comp, keyGen, valGen);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SelectManyIterator<Iterator<T>, TInner, T, TOut> SelectMany<TInner, TOut>(Func<T, TInner> flattener)
        where TInner : IRefIterator<TInner, TOut>, allows ref struct => new(this, flattener);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SelectManyIterator<Iterator<T>, SpanIterator<TOut>, T, TOut> SelectMany<TOut>(Func<T, TOut[]> flattener)
        => new(this, x => flattener(x));

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SelectManyIterator<Iterator<T>, SpanIterator<TOut>, T, TOut> SelectMany<TOut>(
        Func<T, List<TOut>> flattener)
        => new(this, x => flattener(x));

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SelectManyIterator<Iterator<T>, Iterator<TOut>, T, TOut> SelectMany<TOut>(
        Func<T, IEnumerable<TOut>> flattener) => new(this, x => new(flattener(x)));

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryTakeRange(Range r, out Iterator<T> result)
    {
        if (!_imp.TryTakeRange(r, out var newImp))
        {
            Unsafe.SkipInit(out result);
            return false;
        }

        result = new Iterator<T>(newImp);
        return true;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DistinctIterator<Iterator<T>, T> Distinct()
        => Distinct(null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DistinctIterator<Iterator<T>, T> Distinct(IEqualityComparer<T>? comp)
        => new(this, comp);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DistinctIterator<Iterator<T>, T> DistinctBy<TKey>(Func<T, TKey> keySelector) where TKey : notnull
        => new(this, Equality.By(keySelector));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ConcatIterator<Iterator<T>, TOther, T> Concat<TOther>(TOther other)
        where TOther : IRefIterator<TOther, T>, allows ref struct
        => new(this, other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ConcatIterator<Iterator<T>, ItemIterator<T>, T> Append(T append)
        => new(this, append);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ConcatIterator<ItemIterator<T>, Iterator<T>, T> Prepend(T prepend)
        => new(prepend, this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Iterator<T> Empty() => new([]);


    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Min()
        => Min(null);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Max()
        => Max(null);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? MinOrDefault()
        => MinOrDefault(null);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? MaxOrDefault()
        => MaxOrDefault(null);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Min(IComparer<T>? comp)
        => Iterator.TryGetMin(this, comp, out var min) ? min : ThrowHelper.ThrowInvalidOperationException<T>();

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Max(IComparer<T>? comp)
        => Iterator.TryGetMax(this, comp, out var min) ? min : ThrowHelper.ThrowInvalidOperationException<T>();

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? MinOrDefault(IComparer<T>? comp)
    {
        Iterator.TryGetMin(this, comp, out var min);
        return min;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? MaxOrDefault(IComparer<T>? comp)
    {
        Iterator.TryGetMax(this, comp, out var min);
        return min;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T MinBy<TKey>(Func<T, TKey> bySel) where TKey : IComparable<TKey> => Min(Equality.CompareBy(bySel));

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T MaxBy<TKey>(Func<T, TKey> bySel) where TKey : IComparable<TKey> => Max(Equality.CompareBy(bySel));

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? MinOrDefaultBy<TKey>(Func<T, TKey> bySel) where TKey : IComparable<TKey>
        => MinOrDefault(Equality.CompareBy(bySel));

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? MaxOrDefaultBy<T1>(Func<T, T1> bySel) where T1 : IComparable<T1> =>
        MaxOrDefault(Equality.CompareBy(bySel));

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}