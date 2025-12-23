using System.Collections;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;

namespace MeshWiz.RefLinq;

public ref struct OfTypeIterator<TIter, TIn, TOut>(TIter source) : IRefIterator<OfTypeIterator<TIter, TIn, TOut>, TOut>
    where TIter : IRefIterator<TIter, TIn>, allows ref struct
{
    private readonly TIter _source = source;

    private TOut? _current;


    /// <inheritdoc />
    public bool MoveNext()
    {
        while (_source.MoveNext())
        {
            if(_source.Current is not TOut cur) continue;
            _current = cur;
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _source.Reset();
    }

    /// <inheritdoc />
    public TOut Current => _current!;

    /// <inheritdoc />
    object? IEnumerator.Current => _current;

    /// <inheritdoc />
    public void Dispose() => _source.Dispose();

    /// <inheritdoc />
    public TOut First()
        => Iterator.First<OfTypeIterator<TIter, TIn, TOut>, TOut>(this);

    /// <inheritdoc />
    public TOut? FirstOrDefault()
    {
        TryGetFirst(out var first);
        return first;
    }

    /// <inheritdoc />
    public bool TryGetFirst(out TOut? item) => Iterator.TryGetFirst(this, out item);

    /// <inheritdoc />
    public TOut Last()
        => Iterator.Last<OfTypeIterator<TIter, TIn, TOut>, TOut>(this);

    /// <inheritdoc />
    public TOut? LastOrDefault()
    {
        TryGetLast(out var first);
        return first;
    }

    /// <inheritdoc />
    public bool TryGetLast(out TOut? item)
    {
        if (!_source.TryConvertToSpanIter<TIter, TIn>(out var spanIterator)) 
            return Iterator.TryGetLast(this, out item);
        var sp = spanIterator.OriginalSource;
        for (var i = sp.Length- 1; i >= 0; i--)
        {
            if(sp[i] is not TOut found) continue;
            item = found;
            return true;
        }

        item = default;
        return false;
    }

    /// <inheritdoc />
    public int Count() => Iterator.Count<OfTypeIterator<TIter, TIn, TOut>, TOut>(this);

    /// <inheritdoc />
    public bool TryGetNonEnumeratedCount(out int count)
    {
        count = 0;
        return false;
    }

    /// <inheritdoc />
    public void CopyTo(Span<TOut> destination) => Iterator.CopyTo(this,destination);

    /// <inheritdoc />
    public OfTypeIterator<TIter, TIn, TOut> GetEnumerator()
    {
        var copy = this;
        copy.Reset();
        return copy;
    }

    /// <inheritdoc />
    public WhereIterator<OfTypeIterator<TIter, TIn, TOut>, TOut> Where(Func<TOut, bool> predicate) => new(this, predicate);

    public SelectManyIterator<OfTypeIterator<TIter,TIn,TOut>, TInner, TOut, TMany> SelectMany<TInner, TMany>(
        Func<TOut, TInner> flattener) where TInner : IRefIterator<TInner,TMany>, allows ref struct =>
        new(this, flattener);

    public SelectManyIterator<OfTypeIterator<TIter,TIn,TOut>, SpanIterator<TOut2>, TOut, TOut2> SelectMany<TOut2>(
        Func<TOut, SpanIterator<TOut2>> flattener)
        => new(this, flattener);

    public SelectManyIterator<OfTypeIterator<TIter,TIn,TOut>, SpanIterator<TMany>, TOut, TMany> SelectMany<TMany>(
        Func<TOut, TMany[]> flattener) => new(this, inner => flattener(inner));

    public SelectManyIterator<OfTypeIterator<TIter,TIn,TOut>, SpanIterator<TMany>, TOut, TMany> SelectMany<TMany>(
        Func<TOut, List<TMany>> flattener) => new(this, inner => flattener(inner));

    public SelectManyIterator<OfTypeIterator<TIter,TIn,TOut>, AdapterIterator<TMany>, TOut, TMany> SelectMany<TMany>(
        Func<TOut, IEnumerable<TMany>> flattener) => new(this, Func.Combine(flattener,Iterator.Adapt));

    /// <inheritdoc />
    public SelectIterator<OfTypeIterator<TIter, TIn, TOut>, TOut, TOut1> Select<TOut1>(Func<TOut, TOut1> selector)
        => new(this, selector);

    /// <inheritdoc />
    public RangeIterator<OfTypeIterator<TIter, TIn, TOut>, TOut> Take(Range r)
        => Iterator.Take<OfTypeIterator<TIter, TIn, TOut>, TOut>(this, r);

    /// <inheritdoc />
    public RangeIterator<OfTypeIterator<TIter, TIn, TOut>, TOut> Take(int num)
        => Iterator.Take<OfTypeIterator<TIter, TIn, TOut>, TOut>(this, num);

    /// <inheritdoc />
    public RangeIterator<OfTypeIterator<TIter, TIn, TOut>, TOut> Skip(int num)
        => Iterator.Skip<OfTypeIterator<TIter, TIn, TOut>, TOut>(this, num);


    /// <inheritdoc />
    public TOut[] ToArray()
        => Iterator.ToArray<OfTypeIterator<TIter, TIn, TOut>, TOut>(this);

    /// <inheritdoc />
    public List<TOut> ToList() => Iterator.ToList<OfTypeIterator<TIter, TIn, TOut>, TOut>(this);

    /// <inheritdoc />
    public HashSet<TOut> ToHashSet()
        => ToHashSet(EqualityComparer<TOut>.Default);

    /// <inheritdoc />
    public HashSet<TOut> ToHashSet(IEqualityComparer<TOut>? comp)
        => Iterator.ToHashSet(this, comp);

    /// <inheritdoc />
    public TOut First(Func<TOut, bool> predicate)
        => Where(predicate).First();

    /// <inheritdoc />
    public TOut? FirstOrDefault(Func<TOut, bool> predicate)
        => Where(predicate).FirstOrDefault();

    /// <inheritdoc />
    public TOut Last(Func<TOut, bool> predicate)
        => Where(predicate).Last();

    /// <inheritdoc />
    public TOut? LastOrDefault(Func<TOut, bool> predicate)
        => Where(predicate).LastOrDefault();

    /// <inheritdoc />
    public bool Any()
    {
        var copy = this;
        copy.Reset();
        return copy.MoveNext();
    }

    /// <inheritdoc />
    public bool Any(Func<TOut, bool> predicate)
        => Where(predicate).Any();

    /// <inheritdoc />
    public int EstimateCount() => _source.EstimateCount();


    public OfTypeIterator<OfTypeIterator<TIter, TIn, TOut>, TOut, TOther> OfType<TOther>() => new(this);
    
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
    public bool All(Func<TOut,bool> predicate)=>!Any(x=>!predicate(x));

    /// <inheritdoc />
    public bool TryTakeRange(Range r, out OfTypeIterator<TIter, TIn, TOut> result)
    {
        result = default;
        if(!_source.TryTakeRange(r,out var newSource))
            return false;
        result = new OfTypeIterator<TIter, TIn, TOut>(newSource);
        return true;
    }
    
    
    public DistinctIterator<OfTypeIterator<TIter,TIn,TOut>, TOut> Distinct()
        => Distinct(null);
    public DistinctIterator<OfTypeIterator<TIter,TIn,TOut>, TOut> Distinct(IEqualityComparer<TOut>? comp)
        => new(this, comp);
    public DistinctIterator<OfTypeIterator<TIter,TIn,TOut>,TOut> DistinctBy<T>(Func<TOut, T> keySelector) where T : notnull 
        =>new(this,Equality.By(keySelector));

    public ConcatIterator<OfTypeIterator<TIter,TIn,TOut>, TOther, TOut> Concat<TOther>(TOther other) 
        where TOther : IRefIterator<TOther, TOut>,allows ref struct
        => new(this, other);
    public ConcatIterator<OfTypeIterator<TIter,TIn,TOut>, ItemIterator<TOut>, TOut> Append(TOut append) 
        => new(this, append);
    public ConcatIterator<ItemIterator<TOut>,OfTypeIterator<TIter,TIn,TOut>, TOut> Prepend(TOut prepend) 
        => new(prepend,this);

    public static OfTypeIterator<TIter, TIn, TOut> Empty() => new(TIter.Empty());
}