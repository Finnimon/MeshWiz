using System.Collections;
using System.Diagnostics.CodeAnalysis;
using MeshWiz.Utility;

namespace MeshWiz.RefLinq;

public ref struct DistinctIterator<TIter, TItem> : IRefIterator<DistinctIterator<TIter, TItem>, TItem>
    where TIter : IRefIterator<TIter, TItem>, allows ref struct
{
    private readonly HashSet<TItem> _set;
    private TIter _source;
    private TItem? _current;
    public TItem Current => _current!;

    public DistinctIterator(TIter source, IEqualityComparer<TItem>? comp = null)
    {
        _source = source;
        _set = new HashSet<TItem>(comp);
    }

    public bool MoveNext()
    {
        while (_source.MoveNext())
        {
            var cur = _source.Current;
            if (!_set.Add(cur))
                continue;
            _current = cur;
            return true;
        }

        return false;
    }

    public void Reset()
    {
        _current = default;
        _source.Reset();
        _set.Clear();
    }

    /// <inheritdoc />
    object? IEnumerator.Current => Current;

    public void Dispose()
    {
        _source.Dispose();
        _current = default;
    }

    /// <inheritdoc />
    public TItem First()
        => _source.First();

    /// <inheritdoc />
    public TItem? FirstOrDefault()
        => _source.FirstOrDefault();

    /// <inheritdoc />
    public bool TryGetFirst(out TItem? item)
        => _source.TryGetFirst(out item);

    /// <inheritdoc />
    public TItem Last()
        => Iterator.Last<DistinctIterator<TIter, TItem>, TItem>(this);

    /// <inheritdoc />
    public TItem? LastOrDefault()
    {
        TryGetLast(out var last);
        return last;
    }

    /// <inheritdoc />
    public bool TryGetLast(out TItem? item)
        => Iterator.TryGetLast(this, out item);

    /// <inheritdoc />
    public int Count()
        => Iterator.Count<DistinctIterator<TIter, TItem>, TItem>(this);

    /// <inheritdoc />
    public bool TryGetNonEnumeratedCount(out int count)
        => _source.TryGetNonEnumeratedCount(out count) && count is 0 or 1;

    /// <inheritdoc />
    public void CopyTo(Span<TItem> destination)
        => Iterator.CopyTo(this, destination);

    /// <inheritdoc />
    public DistinctIterator<TIter, TItem> GetEnumerator()
    {
        var copy = this;
        copy.Reset();
        return copy;
    }

    /// <inheritdoc />
    public WhereIterator<DistinctIterator<TIter, TItem>, TItem> Where(Func<TItem, bool> predicate)
        => new(this, predicate);

    /// <inheritdoc />
    public SelectIterator<DistinctIterator<TIter, TItem>, TItem, TOut> Select<TOut>(Func<TItem, TOut> selector)
        => new(this, selector);

    /// <inheritdoc />
    public RangeIterator<DistinctIterator<TIter, TItem>, TItem> Take(Range r)
        => new(this, r);

    /// <inheritdoc />
    public RangeIterator<DistinctIterator<TIter, TItem>, TItem> Take(int num)
        => new(this, ..num);

    /// <inheritdoc />
    public RangeIterator<DistinctIterator<TIter, TItem>, TItem> Skip(int num)
        => new(this, num..);

    /// <inheritdoc />
    public TItem[] ToArray()
        => Iterator.ToArray<DistinctIterator<TIter, TItem>, TItem>(this);

    /// <inheritdoc />
    public List<TItem> ToList()
        => Iterator.ToList<DistinctIterator<TIter, TItem>, TItem>(this);

    /// <inheritdoc />
    public HashSet<TItem> ToHashSet()
        => _source.ToHashSet(_set.Comparer);

    /// <inheritdoc />
    public HashSet<TItem> ToHashSet(IEqualityComparer<TItem>? comp)
        => Iterator.ToHashSet(this, comp);

    /// <inheritdoc />
    public TItem First(Func<TItem, bool> predicate)
        => Where(predicate).First();

    /// <inheritdoc />
    public TItem? FirstOrDefault(Func<TItem, bool> predicate)
        => Where(predicate).FirstOrDefault();

    /// <inheritdoc />
    public TItem Last(Func<TItem, bool> predicate)
        => Where(predicate).Last();

    /// <inheritdoc />
    public TItem? LastOrDefault(Func<TItem, bool> predicate)
        => Where(predicate).FirstOrDefault();

    /// <inheritdoc />
    public bool Any()
        => _source.Any();

    /// <inheritdoc />
    public bool Any(Func<TItem, bool> predicate)
        => Where(predicate).Any();

    /// <inheritdoc />
    public bool All(Func<TItem, bool> predicate)
        => !Any(Func.Invert(predicate));

    /// <inheritdoc />
    public int EstimateCount()
        => _source.EstimateCount() / 2;

    /// <inheritdoc />
    public OfTypeIterator<DistinctIterator<TIter, TItem>, TItem, TOther> OfType<TOther>()
        => new(this);

    /// <inheritdoc />
    public TItem Aggregate(Func<TItem, TItem, TItem> aggregator)
        => Iterator.Aggregate(this, aggregator);

    /// <inheritdoc />
    public TOther Aggregate<TOther>(Func<TOther, TItem, TOther> aggregator, TOther seed)
        => Iterator.Aggregate(this, aggregator, seed);

    /// <inheritdoc />
    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(Func<TItem, TKey> keyGen, Func<TItem, TValue> valGen)
        where TKey : notnull
        => ToDictionary(keyGen, valGen, null);

    /// <inheritdoc />
    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(Func<TItem, TKey> keyGen, Func<TItem, TValue> valGen,
        IEqualityComparer<TKey>? comp)
        where TKey : notnull
        => Iterator.ToDictionary(this, comp, keyGen, valGen);

    /// <inheritdoc />
    public Dictionary<TKey, TItem> ToDictionary<TKey>(Func<TItem, TKey> keyGen, IEqualityComparer<TKey>? comp)
        where TKey : notnull
        => ToDictionary(keyGen, Func.Identity, comp);

    /// <inheritdoc />
    public Dictionary<TKey, TItem> ToDictionary<TKey>(Func<TItem, TKey> keyGen) where TKey : notnull
        => ToDictionary(keyGen, Func.Identity, null);

    /// <inheritdoc />
    public SelectManyIterator<DistinctIterator<TIter, TItem>, TInner, TItem, TOut>
        SelectMany<TInner, TOut>(Func<TItem, TInner> flattener)
        where TInner : IRefIterator<TInner, TOut>, allows ref struct
        => new(this, flattener);

    /// <inheritdoc />
    public SelectManyIterator<DistinctIterator<TIter, TItem>, SpanIterator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, TOut[]> flattener)
        => new(this, x => flattener(x));

    /// <inheritdoc />
    public SelectManyIterator<DistinctIterator<TIter, TItem>, SpanIterator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, List<TOut>> flattener)
        => new(this, x => flattener(x));

    /// <inheritdoc />
    public SelectManyIterator<DistinctIterator<TIter, TItem>, AdapterIterator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, IEnumerable<TOut>> flattener)
        => new(this, Func.Combine(flattener, Iterator.Adapt));

    /// <inheritdoc />
    public bool TryTakeRange(Range r, [AllowNull] out DistinctIterator<TIter, TItem> result)
    {
        result = default;
        return false;
    }

    public DistinctIterator<DistinctIterator<TIter, TItem>, TItem> Distinct()
        => Distinct(null);
    public DistinctIterator<DistinctIterator<TIter, TItem>, TItem> Distinct(IEqualityComparer<TItem>? comp)
        => new(this, comp);
    public DistinctIterator<DistinctIterator<TIter,TItem>,TItem> DistinctBy<T>(Func<TItem, T> keySelector) where T : notnull 
        =>new(this,Equality.By(keySelector));

    public ConcatIterator<DistinctIterator<TIter, TItem>, TOther, TItem> Concat<TOther>(TOther other) 
        where TOther : IRefIterator<TOther, TItem>,allows ref struct
        => new(this, other);
    public ConcatIterator<DistinctIterator<TIter, TItem>, ItemIterator<TItem>, TItem> Append(TItem append) 
        => new(this, append);
    public ConcatIterator<ItemIterator<TItem>,DistinctIterator<TIter, TItem>, TItem> Prepend(TItem prepend) 
        => new(prepend,this);


}