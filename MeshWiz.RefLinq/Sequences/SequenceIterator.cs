using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;

namespace MeshWiz.RefLinq;

public struct SequenceIterator<T> : IReadOnlyList<T>, IList<T>, IRefIterator<SequenceIterator<T>, T>
    where T : struct, INumber<T>
{
    private readonly T _start, _endInclusive, _step;
    private readonly int _size;
    private T _pos;

    public SequenceIterator(T start, T endInclusive, T step)
    {
        _start = start;
        _endInclusive = endInclusive;
        _step = step;
        _size = FindSizeAndValidate(start, endInclusive, step);
        _pos = _start - step;
    }

    private static int FindSizeAndValidate(T start, T endInclusive, T step)
    {
        var range = endInclusive - start;
        if (range == T.Zero) return 1;
        if (T.Sign(range) != T.Sign(step))
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(step), step, "Sign error");
        var steps = range / step;
        return int.Max(1, int.CreateTruncating(steps));
    }

    public readonly T Current => _pos;
    public bool MoveNext() => (_pos += _step) <= _endInclusive;
    public void Reset() => _pos = _start - _step;

    /// <inheritdoc />
    readonly object? IEnumerator.Current => Current;

    public readonly SequenceIterator<T> GetEnumerator()
    {
        var copy = this;
        copy.Reset();
        return copy;
    }

    /// <inheritdoc />
    public readonly WhereIterator<SequenceIterator<T>, T> Where(Func<T, bool> predicate) => new(this, predicate);

    /// <inheritdoc />
    public readonly SelectIterator<SequenceIterator<T>, T, TOut> Select<TOut>(Func<T, TOut> selector)
        => new(this, selector);


    public readonly SequenceIterator<T> Take(Range r)
    {
        if (!TryTakeRange(r, out var range))
            ThrowHelper.ThrowInvalidOperationException();
        return range;
    }

    public readonly SequenceIterator<T> Take(int num) => Take(..num);
    public readonly SequenceIterator<T> Skip(int num) => Take(num..);

    /// <inheritdoc />
    readonly RangedIterator<SequenceIterator<T>, T> IRefIterator<SequenceIterator<T>, T>.Take(Range r) => new(this, r);

    /// <inheritdoc />
    readonly RangedIterator<SequenceIterator<T>, T> IRefIterator<SequenceIterator<T>, T>.Take(int num)
        => new(this, ..num);

    /// <inheritdoc />
    readonly RangedIterator<SequenceIterator<T>, T> IRefIterator<SequenceIterator<T>, T>.Skip(int num)
        => new(this, num..);

    /// <inheritdoc />
    readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc />
    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    readonly void ICollection<T>.Add(T item) => ThrowHelper.ThrowNotSupportedException();

    /// <inheritdoc />
    readonly void ICollection<T>.Clear() => ThrowHelper.ThrowNotSupportedException();

    /// <inheritdoc />
    public readonly bool Contains(T item) => item >= _start && item < _endInclusive;
    // => item.InsideInclusiveRange(_start,_end-T.One);

    public readonly void CopyTo(T[] array, int arrayIndex)
        => CopyTo(array.AsSpan(arrayIndex));

    ///
    /// <inheritdoc />
    readonly bool ICollection<T>.Remove(T item)
        => ThrowHelper.ThrowNotSupportedException<bool>();

    public readonly int Count => _size;

    /// <inheritdoc />
    readonly bool ICollection<T>.IsReadOnly => true;

    /// <inheritdoc />
    public readonly int IndexOf(T item)
    {
        if (!Contains(item)) return -1;
        var pos = item - _start;
        return int.CreateTruncating(pos);
    }

    /// <inheritdoc />
    readonly void IList<T>.Insert(int index, T item)
        => ThrowHelper.ThrowNotSupportedException();

    /// <inheritdoc />
    readonly void IList<T>.RemoveAt(int index)
        => ThrowHelper.ThrowNotSupportedException();

    /// <inheritdoc />
    readonly T IList<T>.this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this[index];
        set => ThrowHelper.ThrowNotSupportedException();
    }

    /// <inheritdoc />
    public readonly T this[int index] => GetItem(index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly T GetItem(int index)
    {
        if (Count < (uint)index) IndexThrowHelper.Throw();
        return T.CreateTruncating(index) + _start;
    }

    readonly void IDisposable.Dispose() { }


    /// <inheritdoc />
    public readonly T First()
    {
        if (!TryGetFirst(out var first)) ThrowHelper.ThrowInvalidOperationException();
        return first!;
    }

    /// <inheritdoc />
    public readonly T FirstOrDefault()
    {
        TryGetFirst(out var first);
        return first;
    }

    /// <inheritdoc />
    public readonly bool TryGetFirst(out T item)
    {
        item = _start;
        return _size != 0;
    }

    /// <inheritdoc />
    public readonly T Last()
    {
        if (!TryGetLast(out var last)) ThrowHelper.ThrowInvalidOperationException();
        return last;
    }

    /// <inheritdoc />
    public readonly T LastOrDefault()
    {
        TryGetLast(out var last);
        return last;
    }

    /// <inheritdoc />
    public readonly bool TryGetLast([NotNullWhen(true)] out T item)
    {
        if (_size != 0)
        {
            item = GetItem(_size - 1);
            return true;
        }

        item = default;
        return false;
    }

    /// <inheritdoc />
    readonly int IRefIterator<SequenceIterator<T>, T>.Count() => _size;

    /// <inheritdoc />
    public readonly bool TryGetNonEnumeratedCount(out int count)
    {
        count = _size;
        return true;
    }

    public readonly void CopyTo(Span<T> destination)
    {
        var count = _size;
        if (destination.Length < count) ThrowHelper.ThrowArgumentException(nameof(destination));
        Iterator.FillIncrementing(destination[..count], _start, _step);
    }

    public readonly T[] ToArray()
    {
        var arr = GC.AllocateUninitializedArray<T>(Count);
        CopyTo(arr.AsSpan());
        return arr;
    }

    public readonly List<T> ToList()
    {
        var count = _size;
        List<T> l = new(count);
        CollectionsMarshal.SetCount(l, count);
        CopyTo(CollectionsMarshal.AsSpan(l));
        return l;
    }

    /// <inheritdoc />
    public readonly HashSet<T> ToHashSet()
        => ToHashSet(null);

    /// <inheritdoc />
    public readonly HashSet<T> ToHashSet(IEqualityComparer<T>? comp) => new(this, comp);

    /// <inheritdoc />
    public bool Any() => _size !=0;

    /// <inheritdoc />
    readonly int IRefIterator<SequenceIterator<T>, T>.EstimateCount() => Count;

    /// <inheritdoc />
    public readonly OfTypeIterator<SequenceIterator<T>, T, TOther> OfType<TOther>()
        => new OfTypeIterator<SequenceIterator<T>, T, TOther>(this);

    /// <inheritdoc />
    public readonly T Aggregate(Func<T, T, T> aggregator)
        => Iterator.Aggregate(this, aggregator);

    /// <inheritdoc />
    public readonly TOther Aggregate<TOther>(Func<TOther, T, TOther> aggregator, TOther seed)
        => Iterator.Aggregate(this, aggregator, seed);

    /// <inheritdoc />
    public readonly Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(Func<T, TKey> keyGen, Func<T, TValue> valGen,
        IEqualityComparer<TKey>? comp) where TKey : notnull
        => Iterator.ToDictionary(this, comp, keyGen, valGen);

    /// <inheritdoc />
    public readonly SelectManyIterator<SequenceIterator<T>, TInner, T, TOut> SelectMany<TInner, TOut>(
        Func<T, TInner> flattener)
        where TInner : IRefIterator<TInner, TOut>, allows ref struct
        => new(this, flattener);

    /// <inheritdoc />
    public readonly SelectManyIterator<SequenceIterator<T>, SpanIterator<TOut>, T, TOut> SelectMany<TOut>(
        Func<T, TOut[]> flattener)
        => new(this, x => flattener(x));

    /// <inheritdoc />
    public readonly SelectManyIterator<SequenceIterator<T>, SpanIterator<TOut>, T, TOut> SelectMany<TOut>(
        Func<T, List<TOut>> flattener)
        => new(this, x => flattener(x));

    /// <inheritdoc />
    public readonly SelectManyIterator<SequenceIterator<T>, Iterator<TOut>, T, TOut> SelectMany<TOut>(
        Func<T, IEnumerable<TOut>> flattener)
        => new(this, x => flattener(x).Iterate());

    /// <inheritdoc />
    public readonly bool TryTakeRange(Range r, out SequenceIterator<T> result)
    {
        var (offset, length) = r.GetOffsetAndLength(_size);
        if (length == 0)
        {
            result = Empty();
            return true;
        }

        var start = _start + T.CreateTruncating(offset);
        var endInclusive = start + T.CreateTruncating(length) * _step + _step / (T.One + T.One);
        result = new SequenceIterator<T>(start, endInclusive, _step);
        return true;
    }

    /// <inheritdoc />
    readonly DistinctIterator<SequenceIterator<T>, T> IRefIterator<SequenceIterator<T>, T>.Distinct()
        => Distinct(null);

    /// <inheritdoc />
    public readonly DistinctIterator<SequenceIterator<T>, T> Distinct(IEqualityComparer<T>? comp)
        => new(this, comp);

    /// <inheritdoc />
    public readonly DistinctIterator<SequenceIterator<T>, T> DistinctBy<T1>(Func<T, T1> keySelector) where T1 : notnull
        => Distinct(Equality.By(keySelector));

    /// <inheritdoc />
    public readonly ConcatIterator<SequenceIterator<T>, TOther, T> Concat<TOther>(TOther other)
        where TOther : IRefIterator<TOther, T>, allows ref struct
        => new(this, other);

    /// <inheritdoc />
    public readonly ConcatIterator<SequenceIterator<T>, ItemIterator<T>, T> Append(T append)
        => new(this, append);

    /// <inheritdoc />
    public readonly ConcatIterator<ItemIterator<T>, SequenceIterator<T>, T> Prepend(T prepend)
        => new(prepend, this);

    /// <inheritdoc />
    public static SequenceIterator<T> Empty()
        => new();

    public SequenceIterator()
    {
        _start = T.One;
        _size = 0;
        _endInclusive = T.Zero;
        _pos = T.Zero;
        _step = T.Zero;
    }

    /// <inheritdoc />
    public readonly T Min() => T.IsPositive(_step) ? Last() : First();

    /// <inheritdoc />
    public readonly T Max()
        => T.IsNegative(_step) ? Last() : First();

    /// <inheritdoc />
    public readonly T MinOrDefault() => _size == 0 ? default : Min();

    /// <inheritdoc />
    public readonly T MaxOrDefault() => _size == 0 ? default : Max();

    /// <inheritdoc />
    public readonly T Min(IComparer<T>? comp)
    {
        if (_size == 0) ThrowHelper.ThrowInvalidOperationException();
        if (comp is null || ReferenceEquals(comp, Comparer<T>.Default)) return _start;
        Iterator.TryGetMin(this, comp, out var min);
        return min!; //is determined because of earlier check
    }

    /// <inheritdoc />
    public readonly T Max(IComparer<T>? comp)
    {
        if (_size == 0) ThrowHelper.ThrowInvalidOperationException();
        if (comp is null || ReferenceEquals(comp, Comparer<T>.Default)) return _endInclusive;
        Iterator.TryGetMax(this, comp, out var max);
        return max!; //is determined because of earlier check
    }

    /// <inheritdoc />
    public readonly T MinOrDefault(IComparer<T>? comp)
    {
        if (_size == 0) return default;
        if (comp is null || ReferenceEquals(comp, Comparer<T>.Default)) return Min();
        Iterator.TryGetMin(this, comp, out var min);
        return min; //is determined because of earlier check
    }

    /// <inheritdoc />
    public readonly T MaxOrDefault(IComparer<T>? comp)
    {
        if (_size == 0) return default;
        if (comp is null || ReferenceEquals(comp, Comparer<T>.Default)) return Max();
        Iterator.TryGetMax(this, comp, out var max);
        return max;
    }

    /// <inheritdoc />
    public readonly T MinBy<TKey>(Func<T, TKey> bySel) where TKey : IComparable<TKey> => Min(Equality.CompareBy(bySel));

    /// <inheritdoc />
    public readonly T MaxBy<TKey>(Func<T, TKey> bySel) where TKey : IComparable<TKey> => Max(Equality.CompareBy(bySel));

    /// <inheritdoc />
    public readonly T MinOrDefaultBy<TKey>(Func<T, TKey> bySel) where TKey : IComparable<TKey> =>
        MinOrDefault(Equality.CompareBy(bySel));

    /// <inheritdoc />
    public readonly T MaxOrDefaultBy<TKey>(Func<T, TKey> bySel) where TKey : IComparable<TKey> =>
        MaxOrDefault(Equality.CompareBy(bySel));
}