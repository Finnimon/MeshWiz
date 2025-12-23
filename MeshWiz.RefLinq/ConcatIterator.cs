using System.Collections;
using MeshWiz.Utility;

namespace MeshWiz.RefLinq;

public ref struct ConcatIterator<TLeft, TRight, TItem>(TLeft l, TRight r)
    : IRefIterator<ConcatIterator<TLeft, TRight, TItem>, TItem>
    where TLeft : IRefIterator<TLeft, TItem>, allows ref struct
    where TRight : IRefIterator<TRight, TItem>, allows ref struct
{
    private TLeft _left = l;
    private TRight _right = r;
    private bool _isLeft = true;

    public bool MoveNext()
    {
        if (_isLeft && _left.MoveNext())
            return true;
        _isLeft = false;
        return _right.MoveNext();
    }

    /// <inheritdoc />
    public void Reset()
    {
        _left.Reset();
        _right.Reset();
        _isLeft = true;
    }

    /// <inheritdoc />
    public TItem Current => _isLeft ? _left.Current : _right.Current;

    /// <inheritdoc />
    object? IEnumerator.Current => Current;

    /// <inheritdoc />
    public void Dispose()
    {
        _left.Dispose();
        _right.Dispose();
    }

    /// <inheritdoc />
    public TItem First()
        => Iterator.First<ConcatIterator<TLeft, TRight, TItem>, TItem>(this);

    /// <inheritdoc />
    public TItem? FirstOrDefault()
    {
        TryGetFirst(out var first);
        return first;
    }

    /// <inheritdoc />
    public bool TryGetFirst(out TItem? item)
        => _left.TryGetFirst(out item) || _right.TryGetFirst(out item);

    /// <inheritdoc />
    public TItem Last()
        => Iterator.Last<ConcatIterator<TLeft, TRight, TItem>, TItem>(this);

    /// <inheritdoc />
    public TItem? LastOrDefault()
    {
        TryGetLast(out var last);
        return last;
    }

    /// <inheritdoc />
    public bool TryGetLast(out TItem? item)
        => _right.TryGetLast(out item) || _left.TryGetLast(out item);

    /// <inheritdoc />
    public int Count() => _left.Count() + _right.Count();

    /// <inheritdoc />
    public bool TryGetNonEnumeratedCount(out int count)
    {
        var found = _left.TryGetNonEnumeratedCount(out var lCount)
                    & _right.TryGetNonEnumeratedCount(out var rCount);
        count = lCount + rCount;
        return found;
    }

    /// <inheritdoc />
    public void CopyTo(Span<TItem> destination)
    {
        if (!_left.TryGetNonEnumeratedCount(out var lCount))
        {
            Iterator.CopyTo(this, destination);
            return;
        }

        _left.CopyTo(destination);
        _right.CopyTo(destination[lCount..]);
    }

    /// <inheritdoc />
    public ConcatIterator<TLeft, TRight, TItem> GetEnumerator()
    {
        var copy = this;
        copy.Reset();
        return copy;
    }

    /// <inheritdoc />
    public WhereIterator<ConcatIterator<TLeft, TRight, TItem>, TItem> Where(Func<TItem, bool> predicate)
        => new(this, predicate);

    /// <inheritdoc />
    public SelectIterator<ConcatIterator<TLeft, TRight, TItem>, TItem, TOut> Select<TOut>(Func<TItem, TOut> selector)
        => new(this, selector);

    /// <inheritdoc />
    public RangeIterator<ConcatIterator<TLeft, TRight, TItem>, TItem> Take(Range r)
        => new(this, r);

    /// <inheritdoc />
    public RangeIterator<ConcatIterator<TLeft, TRight, TItem>, TItem> Take(int num)
        => new(this, ..num);

    /// <inheritdoc />
    public RangeIterator<ConcatIterator<TLeft, TRight, TItem>, TItem> Skip(int num)
        => new(this, num..);


    /// <inheritdoc />
    public TItem[] ToArray()
    {
        SegmentedArrayBuilder<TItem> b = new(default);
        b.AddNonICollectionRangeInlined(_left);
        b.AddNonICollectionRangeInlined(_right);
        return b.ToArray();
    }

    /// <inheritdoc />
    public List<TItem> ToList()
    {
        SegmentedArrayBuilder<TItem> b = new(default);
        b.AddNonICollectionRangeInlined(_left);
        b.AddNonICollectionRangeInlined(_right);
        return b.ToList();
    }

    /// <inheritdoc />
    public HashSet<TItem> ToHashSet()
        => ToHashSet(null);

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
        => Where(predicate).LastOrDefault();


    /// <inheritdoc />
    public bool Any()
    {
        var copy = this;
        copy.Reset();
        return copy.MoveNext();
    }

    /// <inheritdoc />
    public bool Any(Func<TItem, bool> predicate) => Where(predicate).MoveNext();

    /// <inheritdoc />
    public bool All(Func<TItem, bool> predicate) => !Any(Func.Invert(predicate));

    /// <inheritdoc />
    public int EstimateCount()
        => _left.EstimateCount() + _right.EstimateCount();

    /// <inheritdoc />
    public OfTypeIterator<ConcatIterator<TLeft, TRight, TItem>, TItem, TOther> OfType<TOther>()
        => new(this);

    /// <inheritdoc />
    public TItem Aggregate(Func<TItem, TItem, TItem> aggregator) => Iterator.Aggregate(this, aggregator);

    /// <inheritdoc />
    public TOther Aggregate<TOther>(Func<TOther, TItem, TOther> aggregator, TOther seed) =>
        Iterator.Aggregate(this, aggregator, seed);

    /// <inheritdoc />
    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(Func<TItem, TKey> keyGen, Func<TItem, TValue> valGen)
        where TKey : notnull =>
        ToDictionary(keyGen, valGen, null);

    /// <inheritdoc />
    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(Func<TItem, TKey> keyGen, Func<TItem, TValue> valGen,
        IEqualityComparer<TKey>? comp) where TKey : notnull
        => Iterator.ToDictionary(this, comp, keyGen, valGen);

    /// <inheritdoc />
    public Dictionary<TKey, TItem> ToDictionary<TKey>(Func<TItem, TKey> keyGen, IEqualityComparer<TKey>? comp)
        where TKey : notnull =>
        ToDictionary(keyGen, x => x, comp);

    /// <inheritdoc />
    public Dictionary<TKey, TItem> ToDictionary<TKey>(Func<TItem, TKey> keyGen) where TKey : notnull
        => ToDictionary(keyGen, x => x, null);

    /// <inheritdoc />
    public SelectManyIterator<ConcatIterator<TLeft, TRight, TItem>, TInner, TItem, TOut>
        SelectMany<TInner, TOut>(Func<TItem, TInner> flattener) where TInner : IRefIterator<TInner,TOut>, allows ref struct =>
        new(this, flattener);

    /// <inheritdoc />
    public SelectManyIterator<ConcatIterator<TLeft, TRight, TItem>, SpanIterator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, TOut[]> flattener)
        => new(this, x => flattener(x));

    
    /// <inheritdoc />
    public SelectManyIterator<ConcatIterator<TLeft, TRight, TItem>, SpanIterator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, SpanIterator<TOut>> flattener)
        => new(this, flattener);

    /// <inheritdoc />
    public SelectManyIterator<ConcatIterator<TLeft, TRight, TItem>, SpanIterator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, List<TOut>> flattener)
        => new(this, x => flattener(x));


    /// <inheritdoc />
    public SelectManyIterator<ConcatIterator<TLeft, TRight, TItem>, AdapterIterator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, IEnumerable<TOut>> flattener)
        => new(this,Func.Combine(flattener,Iterator.Adapt));

    /// <inheritdoc />
    public bool TryTakeRange(Range r, out ConcatIterator<TLeft, TRight, TItem> result)
    {
        result = default;
        return false;
    }
    
    public DistinctIterator<ConcatIterator<TLeft,TRight,TItem>, TItem> Distinct()
        => Distinct(null);
    public DistinctIterator<ConcatIterator<TLeft,TRight,TItem>, TItem> Distinct(IEqualityComparer<TItem>? comp)
        => new(this, comp);
    public DistinctIterator<ConcatIterator<TLeft,TRight,TItem>,TItem> DistinctBy<T>(Func<TItem, T> keySelector) where T : notnull 
        =>new(this,Equality.By(keySelector));

    public ConcatIterator<ConcatIterator<TLeft,TRight,TItem>, TOther, TItem> Concat<TOther>(TOther other) 
        where TOther : IRefIterator<TOther, TItem>,allows ref struct
        => new(this, other);
    public ConcatIterator<ConcatIterator<TLeft,TRight,TItem>, ItemIterator<TItem>, TItem> Append(TItem append) 
        => new(this, append);
    public ConcatIterator<ItemIterator<TItem>,ConcatIterator<TLeft,TRight,TItem>, TItem> Prepend(TItem prepend) 
        => new(prepend,this);
}