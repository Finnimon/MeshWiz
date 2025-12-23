using System.Collections;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.RefLinq;

[StructLayout(LayoutKind.Sequential)]
public struct ItemIterator<T> : IRefIterator<ItemIterator<T>, T>
{
    private Once _once = Bool.Once();
    private readonly bool _empty;
    private readonly T? _item;
    public ItemIterator(T item) : this(item, false) { }

    private ItemIterator(T? item, bool empty = false)
    {
        _item = item;
        _empty = empty;
    }

    /// <inheritdoc />
    public bool MoveNext()
        => _once && !_empty;

    public void Reset()
        => _once = Bool.Once();

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
    public bool TryGetFirst(out T? item1)
    {
        item1 = _item;
        return true;
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
    public int Count() => _empty ? 0 : 1;

    /// <inheritdoc />
    public bool TryGetNonEnumeratedCount(out int count)
    {
        count = Count();
        return true;
    }

    /// <inheritdoc />
    public void CopyTo(Span<T> destination) => destination[0] = _item!;

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
        => _empty ? [] : [_item!];

    /// <inheritdoc />
    public List<T> ToList()
        => _empty ? [] : [_item!];

    /// <inheritdoc />
    public HashSet<T> ToHashSet()
        => _empty ? [] : [_item!];

    /// <inheritdoc />
    public HashSet<T> ToHashSet(IEqualityComparer<T>? comp)
        => _empty ? new(comp) : new(comp) { _item! };

    /// <inheritdoc />
    public int EstimateCount()
        => _empty ? 0 : 1;

    /// <inheritdoc />
    public OfTypeIterator<ItemIterator<T>, T, TOther> OfType<TOther>()
        => new(this);

    /// <inheritdoc />
    public T Aggregate(Func<T, T, T> aggregator)
        => _empty ? ThrowHelper.ThrowInvalidOperationException<T>() : _item!;

    /// <inheritdoc />
    public TOther Aggregate<TOther>(Func<TOther, T, TOther> aggregator, TOther seed)
        => _empty ? seed : aggregator(seed, _item!);

    /// <inheritdoc />
    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(Func<T, TKey> keyGen, Func<T, TValue> valGen,
        IEqualityComparer<TKey>? comp) where TKey : notnull
        => _empty ? new(comp) : new(comp) { { keyGen(_item!), valGen(_item!) } };

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

    public static implicit operator ItemIterator<T>(T item) => new(item);

    public static implicit operator T(ItemIterator<T> iter) =>
        iter._empty ? ThrowHelper.ThrowInvalidOperationException<T>() : iter._item!;

    /// <inheritdoc />
    public bool TryTakeRange(Range r, out ItemIterator<T> result)
    {
        result = this;
        return r.IsAll();
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
    public static ItemIterator<T> Empty() => new(default, true);
}