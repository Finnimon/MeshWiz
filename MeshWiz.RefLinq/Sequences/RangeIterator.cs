using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.RefLinq;

public struct RangeIterator<T> : IReadOnlyList<T>, IList<T>, IEnumerator<T>, IRefIterator<RangeIterator<T>, T>
    where T :struct, INumber<T>
{
    private readonly T _start, _end;
    private readonly int _size;
    private T _pos;

    public RangeIterator(T start, int count)
    {
        _start = start;
        if (count < 0) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(count));
        _end = start + T.CreateTruncating(count);
        _size = count;
        _pos = _start - T.One;
    }

    public readonly T Current => _pos;
    public bool MoveNext() => ++_pos < _end;

    public void Reset() => _pos = _start - T.One;

    /// <inheritdoc />
    readonly object? IEnumerator.Current => Current;

    public readonly RangeIterator<T> GetEnumerator()
    {
        var copy = this;
        copy.Reset();
        return copy;
    }

    /// <inheritdoc />
    public readonly WhereIterator<RangeIterator<T>, T> Where(Func<T, bool> predicate) => new(this, predicate);

    /// <inheritdoc />
    public readonly SelectIterator<RangeIterator<T>, T, TOut> Select<TOut>(Func<T, TOut> selector)
        => new(this, selector);

    
    public readonly RangeIterator<T> Take(Range r)
    {
        if (!TryTakeRange(r, out var range))
            ThrowHelper.ThrowInvalidOperationException();
        return range;
    }

    public readonly RangeIterator<T> Take(int num) => Take(..num);
    public readonly RangeIterator<T> Skip(int num) => Take(num..);
    /// <inheritdoc />
    readonly RangedIterator<RangeIterator<T>, T> IRefIterator<RangeIterator<T>, T>.Take(Range r) => new(this, r);

    /// <inheritdoc />
    readonly RangedIterator<RangeIterator<T>, T> IRefIterator<RangeIterator<T>, T>.Take(int num)
        => new(this,..num);

    /// <inheritdoc />
    readonly RangedIterator<RangeIterator<T>, T> IRefIterator<RangeIterator<T>, T>.Skip(int num)
        => new(this,num..);

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
    public readonly bool Contains(T item) => item >= _start && item < _end;
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
    public readonly T this[int index]
    {
        get
        {
            if (Count < (uint)index) IndexThrowHelper.Throw();
            return T.CreateTruncating(index) + _start;
        }
    }

    readonly void IDisposable.Dispose() { }


    /// <inheritdoc />
    public readonly T First() => Min();

    /// <inheritdoc />
    public readonly T FirstOrDefault() => MinOrDefault();

    /// <inheritdoc />
    public readonly bool TryGetFirst(out T item)
    {
        item = _start;
        return _size != 0;
    }

    /// <inheritdoc />
    public readonly T Last() => Max();

    /// <inheritdoc />
    public readonly T LastOrDefault() => MaxOrDefault();

    /// <inheritdoc />
    public readonly bool TryGetLast(out T item)
    {
        item = _end;
        return _size != 0;
    }

    /// <inheritdoc />
    readonly int IRefIterator<RangeIterator<T>, T>.Count() => _size;

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
        Iterator.FillIncrementing(destination[..count], _start);
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
    public bool Any() => _size != 0;

    /// <inheritdoc />
    readonly int IRefIterator<RangeIterator<T>, T>.EstimateCount() => Count;

    /// <inheritdoc />
    public readonly OfTypeIterator<RangeIterator<T>, T, TOther> OfType<TOther>()
        => new(this);

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
    public readonly SelectManyIterator<RangeIterator<T>, TInner, T, TOut> SelectMany<TInner, TOut>(Func<T, TInner> flattener)
        where TInner : IRefIterator<TInner, TOut>, allows ref struct
        => new(this, flattener);

    /// <inheritdoc />
    public readonly SelectManyIterator<RangeIterator<T>, SpanIterator<TOut>, T, TOut> SelectMany<TOut>(Func<T, TOut[]> flattener)
        => new(this, x => flattener(x));

    /// <inheritdoc />
    public readonly SelectManyIterator<RangeIterator<T>, SpanIterator<TOut>, T, TOut> SelectMany<TOut>(
        Func<T, List<TOut>> flattener)
        => new(this, x => flattener(x));

    /// <inheritdoc />
    public readonly SelectManyIterator<RangeIterator<T>, Iterator<TOut>, T, TOut> SelectMany<TOut>(
        Func<T, IEnumerable<TOut>> flattener)
        => new(this, x => flattener(x).Iterate());

    /// <inheritdoc />
    public readonly bool TryTakeRange(Range r, out RangeIterator<T> result)
    {
        var (offset, length) = r.GetOffsetAndLength(this._size);
        var start = _start + T.CreateTruncating(offset);
        result = new(start, length);
        return true;
    }

    /// <inheritdoc />
    public readonly DistinctIterator<RangeIterator<T>, T> Distinct()
        => Distinct(null);

    /// <inheritdoc />
    public readonly DistinctIterator<RangeIterator<T>, T> Distinct(IEqualityComparer<T>? comp)
        => new(this, comp);

    /// <inheritdoc />
    public readonly DistinctIterator<RangeIterator<T>, T> DistinctBy<T1>(Func<T, T1> keySelector) where T1 : notnull
        => new(this, Equality.By(keySelector));

    /// <inheritdoc />
    public readonly ConcatIterator<RangeIterator<T>, TOther, T> Concat<TOther>(TOther other)
        where TOther : IRefIterator<TOther, T>, allows ref struct
        => new(this, other);

    /// <inheritdoc />
    public readonly ConcatIterator<RangeIterator<T>, ItemIterator<T>, T> Append(T append)
        => new(this, append);

    /// <inheritdoc />
    public readonly ConcatIterator<ItemIterator<T>, RangeIterator<T>, T> Prepend(T prepend)
        => new(prepend, this);

    /// <inheritdoc />
    public static RangeIterator<T> Empty()
        => new(T.Zero, 0);

    /// <inheritdoc />
    public readonly T Min()
    {
        if (_size == 0) ThrowHelper.ThrowInvalidOperationException();
        return _start;
    }

    /// <inheritdoc />
    public readonly T Max()
    {
        if (_size == 0) ThrowHelper.ThrowInvalidOperationException();
        return _end;
    }

    /// <inheritdoc />
    public readonly T MinOrDefault() => _size == 0 ? default : _start;

    /// <inheritdoc />
    public readonly T MaxOrDefault() => _size == 0 ? default : _end;

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
        if (comp is null || ReferenceEquals(comp, Comparer<T>.Default)) return _end;
        Iterator.TryGetMax(this, comp, out var max);
        return max!; //is determined because of earlier check
    }

    /// <inheritdoc />
    public readonly T MinOrDefault(IComparer<T>? comp)
    {
        if (_size == 0) return default;
        if (comp is null || ReferenceEquals(comp, Comparer<T>.Default)) return _start;
        Iterator.TryGetMin(this, comp, out var min);
        return min!; //is determined because of earlier check
    }

    /// <inheritdoc />
    public readonly T MaxOrDefault(IComparer<T>? comp)
    {
        if (_size == 0) return default;
        if (comp is null || ReferenceEquals(comp, Comparer<T>.Default)) return _end;
        Iterator.TryGetMax(this, comp, out var max);
        return max!;
    }

    /// <inheritdoc />
    public readonly T MinBy<TKey>(Func<T, TKey> bySel) where TKey : IComparable<TKey> => Min(Equality.CompareBy(bySel));

    /// <inheritdoc />
    public readonly T MaxBy<TKey>(Func<T, TKey> bySel) where TKey : IComparable<TKey> => Max(Equality.CompareBy(bySel));

    /// <inheritdoc />
    public readonly T MinOrDefaultBy<TKey>(Func<T, TKey> bySel) where TKey : IComparable<TKey> => MinOrDefault(Equality.CompareBy(bySel));

    /// <inheritdoc />
    public readonly T MaxOrDefaultBy<TKey>(Func<T, TKey> bySel) where TKey : IComparable<TKey> => MaxOrDefault(Equality.CompareBy(bySel));
}