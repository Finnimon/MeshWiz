using System.Collections;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.RefLinq;

public ref struct SelectIterator<TIter, TIn,TOut>(TIter source, Func<TIn, TOut> sel)
    : IRefIterator<SelectIterator<TIter, TIn,TOut>, TOut>
    where TIter : IRefIterator<TIter, TIn>, allows ref struct
{
    private TIter _source = source;
    private readonly Func<TIn, TOut> _sel = sel;
    private TOut? _current;


    /// <inheritdoc />
    public bool MoveNext()
    {
        while (_source.MoveNext())
        {
            var cur = _source.Current;
            _current = _sel(cur);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void Reset() => _source.Reset();

    /// <inheritdoc />
    public TOut Current => _current!;

    /// <inheritdoc />
    object? IEnumerator.Current => _current;

    /// <inheritdoc />
    public void Dispose() { }


    /// <inheritdoc />
    public TOut First() => Iterator.First<SelectIterator<TIter,TIn, TOut>, TOut>(this);

    /// <inheritdoc />
    public TOut? FirstOrDefault()
    {
        Iterator.TryGetFirst<SelectIterator<TIter,TIn, TOut>, TOut>(this, out var item);
        return item;
    }

    /// <inheritdoc />
    public bool TryGetFirst(out TOut? item) => Iterator.TryGetFirst(this, out item);

    /// <inheritdoc />
    public TOut Last() 
        => TryGetLast(out var last) ? last! : ThrowHelper.ThrowInvalidOperationException<TOut>();

    /// <inheritdoc />
    public TOut? LastOrDefault()
    {
        TryGetLast(out var item);
        return item;
    }

    /// <inheritdoc />
    public bool TryGetLast(out TOut? item)
    {
        var found= _source.TryGetLast(out var pre);
        item = found ? _sel(pre!) : default;
        return found;
    }


    /// <inheritdoc />
    public int Count() => Iterator.Count<SelectIterator<TIter,TIn, TOut>, TOut>(this);

    /// <inheritdoc />
    public bool TryGetNonEnumeratedCount(out int count) => _source.TryGetNonEnumeratedCount(out count);

    /// <inheritdoc />
    public void CopyTo(Span<TOut> destination) => Iterator.CopyTo(this, destination);

    public readonly SelectIterator<TIter,TIn, TOut> GetEnumerator()
    {
        var copy = this;
        copy.Reset();
        return copy;
    }

    /// <inheritdoc />
    public FilterIterator<SelectIterator<TIter,TIn, TOut>, TOut> Where(Func<TOut, bool> predicate) => new(this, predicate);
    
    
    public SelectManyIterator<SelectIterator<TIter,TIn,TOut>, TInner, TOut, TMany> SelectMany<TInner, TMany>(
        Func<TOut, TInner> flattener) where TInner : IEnumerator<TMany>, allows ref struct =>
        new(this, flattener);

    public SelectManyIterator<SelectIterator<TIter,TIn,TOut>, SpanIterator<TMany>, TOut, TMany> SelectMany<TMany>(
        Func<TOut, TMany[]> flattener) => new(this, inner => flattener(inner));

    public SelectManyIterator<SelectIterator<TIter,TIn,TOut>, SpanIterator<TMany>, TOut, TMany> SelectMany<TMany>(
        Func<TOut, List<TMany>> flattener) => new(this, inner => flattener(inner));

    public SelectManyIterator<SelectIterator<TIter,TIn,TOut>, IEnumerator<TMany>, TOut, TMany> SelectMany<TMany>(
        Func<TOut, IEnumerable<TMany>> flattener) => new(this, inner => flattener(inner).GetEnumerator());

    /// <inheritdoc />
    public SelectIterator<SelectIterator<TIter, TIn, TOut>, TOut, TOut1> Select<TOut1>(Func<TOut, TOut1> selector) 
        => new(this, selector);
    /// <inheritdoc />
    public RangeIterator<SelectIterator<TIter,TIn,TOut>, TOut> Take(Range r) 
        => Iterator.Take<SelectIterator<TIter,TIn,TOut>, TOut>(this, r);

    /// <inheritdoc />
    public RangeIterator<SelectIterator<TIter,TIn,TOut>, TOut> Take(int num)
        => Iterator.Take<SelectIterator<TIter,TIn,TOut>, TOut>(this, num);
    

    /// <inheritdoc />
    public RangeIterator<SelectIterator<TIter,TIn,TOut>, TOut> Skip(int num)
        => Iterator.Skip<SelectIterator<TIter,TIn,TOut>, TOut>(this, num);
    /// <inheritdoc />
    public TOut[] ToArray() => Iterator.ToArray<SelectIterator<TIter, TIn, TOut>, TOut>(this);

    /// <inheritdoc />
    public List<TOut> ToList() => Iterator.ToList<SelectIterator<TIter, TIn, TOut>, TOut>(this);

    /// <inheritdoc />
    public HashSet<TOut> ToHashSet()
        => Iterator.ToHashSet<SelectIterator<TIter, TIn, TOut>, TOut>(this);

    /// <inheritdoc />
    public HashSet<TOut> ToHashSet(IEqualityComparer<TOut> comp)
        => Iterator.ToHashSet(this,comp);
    
    public TOut First(Func<TOut,bool> predicate)=>this.Where(predicate).First();
    public TOut? FirstOrDefault(Func<TOut,bool> predicate)=>this.Where(predicate).FirstOrDefault();

    public TOut Last(Func<TOut,bool> predicate)=>this.Where(predicate).Last();
    public TOut? LastOrDefault(Func<TOut,bool> predicate)=>this.Where(predicate).LastOrDefault();
    
    public bool Any()
    {
        var copy = this;
        return copy.MoveNext();
    }
    public bool Any(Func<TOut,bool> predicate)=>Where(predicate).MoveNext();

    /// <inheritdoc />
    public int EstimateCount()
        => _source.EstimateCount();
    
    public OfTypeIterator<SelectIterator<TIter, TIn, TOut>, TOut, TOther> OfType<TOther>() => new(this);
    
    public TOut Aggregate(Func<TOut, TOut, TOut> aggregator)
    {
        var iter = this;
        iter.Reset();
        if (!iter.MoveNext())
            ThrowHelper.ThrowInvalidOperationException();
        var seed = iter.Current;
        while (iter.MoveNext()) seed = aggregator(seed, iter.Current);
        return seed;
    }

    public TOther Aggregate<TOther>(Func<TOther, TOut, TOther> aggregator, TOther seed)
    {
        var iter = this;
        iter.Reset();
        while (iter.MoveNext()) seed = aggregator(seed, iter.Current);
        return seed;
    }
    
    
    
    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
        Func<TOut, TKey> keyGen, 
        Func<TOut, TValue> valGen) 
        where TKey : notnull =>
        ToDictionary(keyGen, valGen,null);

    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
        Func<TOut, TKey> keyGen, 
        Func<TOut, TValue> valGen,
        IEqualityComparer<TKey>? comp) 
        where TKey : notnull =>
        Iterator.ToDictionary(this,comp,keyGen, valGen);

    public Dictionary<TKey, TOut> ToDictionary<TKey>(
        Func<TOut, TKey> keyGen,
        IEqualityComparer<TKey>? comp)
        where TKey : notnull
        => ToDictionary(keyGen, x => x, comp);
    public Dictionary<TKey, TOut> ToDictionary<TKey>(
        Func<TOut, TKey> keyGen)
        where TKey : notnull
        => ToDictionary(keyGen, x => x, null);
}
