using System.Collections;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.RefLinq;

public ref struct FilterIterator<TIter, TItem>(TIter source, Func<TItem, bool> filter)
    : IRefIterator<FilterIterator<TIter, TItem>, TItem>
    where TIter : IRefIterator<TIter, TItem>, allows ref struct
{
    private TIter _source = source;
    private readonly Func<TItem, bool> _filter = filter;
    private TItem? _current;


    /// <inheritdoc />
    public bool MoveNext()
    {
        while (_source.MoveNext())
        {
            var cur = _source.Current;
            if (!_filter(cur)) continue;
            _current = cur;
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void Reset() => _source.Reset();

    /// <inheritdoc />
    public readonly TItem Current => _current!;

    /// <inheritdoc />
    readonly object? IEnumerator.Current => _current;

    /// <inheritdoc />
    public void Dispose() => _source.Dispose();


    /// <inheritdoc />
    public TItem First() => Iterator.First<FilterIterator<TIter, TItem>, TItem>(this);

    /// <inheritdoc />
    public TItem? FirstOrDefault()
    {
        TryGetFirst(out var item);
        return item;
    }

    /// <inheritdoc />
    public bool TryGetFirst(out TItem? item) => Iterator.TryGetFirst(this, out item);

    /// <inheritdoc />
    public TItem Last() => Iterator.Last<FilterIterator<TIter, TItem>, TItem>(this);

    /// <inheritdoc />
    public TItem? LastOrDefault()
    {
        TryGetLast(out var item);
        return item;
    }

    /// <inheritdoc />
    public bool TryGetLast(out TItem? item)
    {
        if (!_source.TryConvertToSpanIter<TIter, TItem>(out var spanIter))
            return Iterator.TryGetLast(this, out item);
        var sp = spanIter.OriginalSource;
        item = default;
        for (var i = sp.Length - 1; i >= 0; i--)
        {
            item = sp[i];
            var found = _filter(item);
            if (found)
                return true;
        }

        return false;
    }


    /// <inheritdoc />
    public int Count() => Iterator.Count<FilterIterator<TIter, TItem>, TItem>(this);

    /// <inheritdoc />
    public bool TryGetNonEnumeratedCount(out int count) => _source.TryGetNonEnumeratedCount(out count) && count == 0;

    /// <inheritdoc />
    public void CopyTo(Span<TItem> destination) => Iterator.CopyTo(this, destination);

    public readonly FilterIterator<TIter, TItem> GetEnumerator()
    {
        var copy = this;
        copy.Reset();
        return copy;
    }

    /// <inheritdoc />
    public FilterIterator<FilterIterator<TIter, TItem>, TItem> Where(Func<TItem, bool> predicate) =>
        new(this, predicate);

    /// <inheritdoc />
    public SelectIterator<FilterIterator<TIter, TItem>, TItem, TOut> Select<TOut>(Func<TItem, TOut> selector) =>
        new(this, selector);

    /// <inheritdoc />
    public RangeIterator<FilterIterator<TIter, TItem>, TItem> Take(Range r)
        => Iterator.Take<FilterIterator<TIter, TItem>, TItem>(this, r);

    /// <inheritdoc />
    public RangeIterator<FilterIterator<TIter, TItem>, TItem> Take(int num)
        => Iterator.Take<FilterIterator<TIter, TItem>, TItem>(this, num);


    /// <inheritdoc />
    public RangeIterator<FilterIterator<TIter, TItem>, TItem> Skip(int num)
        => Iterator.Skip<FilterIterator<TIter, TItem>, TItem>(this, num);


    /// <inheritdoc />
    public TItem[] ToArray() => Iterator.ToArray<FilterIterator<TIter, TItem>, TItem>(this);

    /// <inheritdoc />
    public List<TItem> ToList() => Iterator.ToList<FilterIterator<TIter, TItem>, TItem>(this);

    /// <inheritdoc />
    public HashSet<TItem> ToHashSet() => Iterator.ToHashSet<FilterIterator<TIter, TItem>, TItem>(this);

    public HashSet<TItem> ToHashSet(IEqualityComparer<TItem> comp)
        => Iterator.ToHashSet(this, comp);

    public TItem First(Func<TItem, bool> predicate) => this.Where(predicate).First();
    public TItem? FirstOrDefault(Func<TItem, bool> predicate) => this.Where(predicate).FirstOrDefault();

    public TItem Last(Func<TItem, bool> predicate) => this.Where(predicate).Last();
    public TItem? LastOrDefault(Func<TItem, bool> predicate) => this.Where(predicate).LastOrDefault();


    public bool Any()
    {
        var copy = this;
        copy.Reset();
        return copy.MoveNext();
    }

    public bool Any(Func<TItem, bool> predicate) => Where(predicate).MoveNext();

    /// <inheritdoc />
    public int EstimateCount()
        => _source.EstimateCount();

    public OfTypeIterator<FilterIterator<TIter, TItem>, TItem, TOther> OfType<TOther>() => new(this);

    public TItem Aggregate(Func<TItem, TItem, TItem> aggregator)
    {
        var iter = this;
        iter.Reset();
        if (!iter.MoveNext())
            ThrowHelper.ThrowInvalidOperationException();
        var seed = iter.Current;
        while (iter.MoveNext()) seed = aggregator(seed, iter.Current);
        return seed;
    }

    public TOther Aggregate<TOther>(Func<TOther, TItem, TOther> aggregator, TOther seed)
    {
        var iter = this;
        iter.Reset();
        while (iter.MoveNext()) seed = aggregator(seed, iter.Current);
        return seed;
    }


    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
        Func<TItem, TKey> keyGen,
        Func<TItem, TValue> valGen)
        where TKey : notnull =>
        ToDictionary(keyGen, valGen, null);

    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
        Func<TItem, TKey> keyGen,
        Func<TItem, TValue> valGen,
        IEqualityComparer<TKey>? comp)
        where TKey : notnull =>
        Iterator.ToDictionary(this, comp, keyGen, valGen);

    public Dictionary<TKey, TItem> ToDictionary<TKey>(
        Func<TItem, TKey> keyGen,
        IEqualityComparer<TKey>? comp)
        where TKey : notnull
        => ToDictionary(keyGen, x => x, comp);

    public Dictionary<TKey, TItem> ToDictionary<TKey>(
        Func<TItem, TKey> keyGen)
        where TKey : notnull
        => ToDictionary(keyGen, x => x, null);
    
    
    public SelectManyIterator<FilterIterator<TIter,TItem>, TInner, TItem, TOut> SelectMany<TInner, TOut>(
        Func<TItem, TInner> flattener) where TInner : IEnumerator<TOut>, allows ref struct =>
        new(this, flattener);

    public SelectManyIterator<FilterIterator<TIter,TItem>, SpanIterator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, TOut[]> flattener) => new(this, inner => flattener(inner));

    public SelectManyIterator<FilterIterator<TIter,TItem>, SpanIterator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, List<TOut>> flattener) => new(this, inner => flattener(inner));

    public SelectManyIterator<FilterIterator<TIter,TItem>, IEnumerator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, IEnumerable<TOut>> flattener) => new(this, inner => flattener(inner).GetEnumerator());

}