using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.RefLinq;

[StructLayout(LayoutKind.Sequential)]
public struct ItemIterator<T> : IRefIterator<ItemIterator<T>, T>
{
    private readonly int _count;
    private int _pos;
    private readonly T? _item;
    public ItemIterator(T item) : this(item, 1) { }

    public ItemIterator(T? item, int count)
    {
        _item = item;
        _count = count;
        _pos = -1;
    }

    /// <inheritdoc />
    public bool MoveNext()
        => ++_pos<_count;

    public void Reset()
        => _pos=-1;

    /// <inheritdoc />
    public T Current => _item!;

    /// <inheritdoc />
    object? IEnumerator.Current => _item;

    /// <inheritdoc />
    public void Dispose() { }

    /// <inheritdoc />
    public T First() => _item!;

    /// <inheritdoc />
    public T? FirstOrDefault() => _item;

    /// <inheritdoc />
    public bool TryGetFirst(out T? item)
    {
        item = _item;
        return _count>0;
    }

    /// <inheritdoc />
    public T Last() => _item!;

    /// <inheritdoc />
    public T? LastOrDefault() => _item;

    /// <inheritdoc />
    public bool TryGetLast(out T? item1)
    {
        item1 = _item;
        return true;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Count() => _count;

    /// <inheritdoc />
    public bool TryGetNonEnumeratedCount(out int count)
    {
        count = _count;
        return true;
    }

    /// <inheritdoc />
    public void CopyTo(Span<T> destination)
    {
        destination[.._count].Fill(_item!);
    }

    /// <inheritdoc />
    public ItemIterator<T> GetEnumerator()
    {
        var copy = this;
        copy.Reset();
        return copy;
    }

    /// <inheritdoc />
    public WhereIterator<ItemIterator<T>, T> Where(Func<T, bool> predicate)
        => new(this, predicate);

    /// <inheritdoc />
    public SelectIterator<ItemIterator<T>, T, TOut> Select<TOut>(Func<T, TOut> selector)
        => new(this, selector);

    /// <inheritdoc />
    public RangeIterator<ItemIterator<T>, T> Take(Range r)
        => new(this, r);

    /// <inheritdoc />
    public RangeIterator<ItemIterator<T>, T> Take(int num)
        => new(this, ..num);

    /// <inheritdoc />
    public RangeIterator<ItemIterator<T>, T> Skip(int num)
        => new(this, num..);

    /// <inheritdoc />
    public T[] ToArray()
        => Iterator.ToArray<ItemIterator<T>, T>(this);

    /// <inheritdoc />
    public List<T> ToList()
        => new(Enumerable.Repeat(_item!,_count));

    /// <inheritdoc />
    public HashSet<T> ToHashSet()
        => ToHashSet(null);

    /// <inheritdoc />
    public HashSet<T> ToHashSet(IEqualityComparer<T>? comp)
        => new(Enumerate(), comp);

    /// <inheritdoc />
    public int EstimateCount()
        => _count;

    /// <inheritdoc />
    public OfTypeIterator<ItemIterator<T>, T, TOther> OfType<TOther>()
        => new(this);

    /// <inheritdoc />
    public T Aggregate(Func<T, T, T> aggregator)
        => Enumerate().Aggregate(aggregator);

    private IEnumerable<T> Enumerate() => Enumerable.Repeat(_item!,_count);

    /// <inheritdoc />
    public TOther Aggregate<TOther>(Func<TOther, T, TOther> aggregator, TOther seed)
        => Enumerate().Aggregate(seed,aggregator);

    /// <inheritdoc />
    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(Func<T, TKey> keyGen, Func<T, TValue> valGen,
        IEqualityComparer<TKey>? comp) where TKey : notnull
        => Enumerate().ToDictionary(keyGen, valGen, comp);
    /// <inheritdoc />
    public SelectManyIterator<ItemIterator<T>, TInner, T, TOut> SelectMany<TInner, TOut>(Func<T, TInner> flattener)
        where TInner : IRefIterator<TInner, TOut>, allows ref struct
        => new(this, flattener);

    /// <inheritdoc />
    public SelectManyIterator<ItemIterator<T>, SpanIterator<TOut>, T, TOut> SelectMany<TOut>(Func<T, TOut[]> flattener)
        => new(this, x => flattener(x));

    /// <inheritdoc />
    public SelectManyIterator<ItemIterator<T>, SpanIterator<TOut>, T, TOut> SelectMany<TOut>(
        Func<T, List<TOut>> flattener)
        => new(this, x => flattener(x));

    /// <inheritdoc />
    public SelectManyIterator<ItemIterator<T>, AdapterIterator<TOut>, T, TOut> SelectMany<TOut>(
        Func<T, IEnumerable<TOut>> flattener)
        => new(this, Func.Combine(flattener, Iterator.Adapt));

    public static implicit operator ItemIterator<T>(T item) => new(item,1);

    public static implicit operator T(ItemIterator<T> iter) => iter._item!;

    /// <inheritdoc />
    public bool TryTakeRange(Range r, out ItemIterator<T> result)
    {
        result = default;
        var res= Func.Try(r.GetOffsetAndLength, _count);
        if(!res) return false;
        var length = res.Value.Length;
        result = new ItemIterator<T>(_item, length);
        return true;
    }


    public DistinctIterator<ItemIterator<T>, T> Distinct()
        => Distinct(null);

    public DistinctIterator<ItemIterator<T>, T> Distinct(IEqualityComparer<T>? comp)
        => new(this, comp);

    public DistinctIterator<ItemIterator<T>, T> DistinctBy<TKey>(Func<T, TKey> keySelector) where TKey : notnull
        => new(this, Equality.By(keySelector));

    public ConcatIterator<ItemIterator<T>, TOther, T> Concat<TOther>(TOther other)
        where TOther : IRefIterator<TOther, T>, allows ref struct
        => new(this, other);

    public ConcatIterator<ItemIterator<T>, ItemIterator<T>, T> Append(T append)
        => new(this, append);

    public ConcatIterator<ItemIterator<T>, ItemIterator<T>, T> Prepend(T prepend)
        => new(prepend, this);

    /// <inheritdoc />
    public static ItemIterator<T> Empty() => new(default, 0);


    /// <inheritdoc />
    public T Min() => First();

    /// <inheritdoc />
    public T Max() => First();

    /// <inheritdoc />
    public T? MinOrDefault() => FirstOrDefault();

    /// <inheritdoc />
    public T? MaxOrDefault()
        => FirstOrDefault();
    /// <inheritdoc />
    public T Min(IComparer<T>? comp)
        => First();
    /// <inheritdoc />
    public T Max(IComparer<T>? comp)
        => First();
    /// <inheritdoc />
    public T? MinOrDefault(IComparer<T>? comp)
        => FirstOrDefault();

    /// <inheritdoc />
    public T? MaxOrDefault(IComparer<T>? comp)
        => FirstOrDefault();

    /// <inheritdoc />
    public T MinBy<TKey>(Func<T, TKey> bySel) where TKey : IComparable<TKey> => First();

    /// <inheritdoc />
    public T MaxBy<TKey>(Func<T, TKey> bySel) where TKey : IComparable<TKey> => First();

    /// <inheritdoc />
    public T? MinOrDefaultBy<TKey>(Func<T, TKey> bySel) where TKey : IComparable<TKey>
        => FirstOrDefault();

    /// <inheritdoc />
    public T? MaxOrDefaultBy<T1>(Func<T, T1> bySel) where T1 : IComparable<T1>
        => FirstOrDefault();
}