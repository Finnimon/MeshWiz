using System.Collections;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;

namespace MeshWiz.RefLinq;

public ref struct RangeIterator<TIter, TItem> : IRefIterator<RangeIterator<TIter,TItem>,TItem>
    where TIter : IRefIterator<TIter, TItem>, allows ref struct

{
    private TIter _source;
    private readonly int _start, _endExcl;
    private int _pos;
    private readonly bool _spanBased;
    private readonly int _sourceCount;

    private RangeIterator(TIter source, Range r, int sourceCount)
    {
        _pos = -1;
        _sourceCount = sourceCount;
        if (source.TryConvertToSpanIter<TIter, TItem>(out var spanIterator))
        {
            spanIterator = spanIterator.OriginalSource[r];
            _source = Unsafe.As<SpanIterator<TItem>, TIter>(ref spanIterator);
            _spanBased = true;
            _start = 0;
            _endExcl = spanIterator.OriginalSource.Length;
            return;
        }

        _source = source;
        _source.Reset();
        _start = r.Start.GetOffset(sourceCount);
        _endExcl = r.End.GetOffset(sourceCount);
        var count = _endExcl - _start;
        if (count < 0||_start<0)
            ThrowHelper.ThrowInvalidOperationException();
    }
    
    /// <inheritdoc />
    public void Reset()
    {
        _pos = -1;
        _source.Reset();
    }

    /// <inheritdoc />
    object? IEnumerator.Current => Current;

    public TItem Current => _source.Current;

    public RangeIterator(TIter source, Range range)
    {
        _pos = -1;
        if (source.TryConvertToSpanIter<TIter, TItem>(out var spanIterator))
        {
            spanIterator = spanIterator.OriginalSource[range];
            _source = Unsafe.As<SpanIterator<TItem>, TIter>(ref spanIterator);
            _spanBased = true;
            _sourceCount = spanIterator.OriginalSource.Length;
            return;
        }

        _source = source;
        var sourceCount = _source.Count();
        _sourceCount= sourceCount;
        _source.Reset();
        _start= range.Start.GetOffset(sourceCount);
        _endExcl = range.End.GetOffset(sourceCount);
        var count = _endExcl - _start;
        if (count < 0||_start<0)
            ThrowHelper.ThrowInvalidOperationException();
    }

    private RangeIterator(TIter source, int start, int end, int sourceCount)
    {
        _pos = -1;
        if (source.TryConvertToSpanIter<TIter, TItem>(out var spanIterator))
        {
            spanIterator = spanIterator.OriginalSource[start..end];
            _source = Unsafe.As<SpanIterator<TItem>, TIter>(ref spanIterator);
            _spanBased = true;
            _sourceCount = spanIterator.OriginalSource.Length;
            return;
        }
        
        _source = source;
        _start = start;
        _endExcl = end;
        _sourceCount = sourceCount;
    }

    public bool MoveNext()
    {
        return _spanBased ? _source.MoveNext() : MoveNextSlow();
    }

    private bool MoveNextSlow()
    {
        while (MoveSource())
            if(_pos>=_start&&_pos<_endExcl) return true;

        return false;
    }

    private bool MoveSource()
    {
        _pos++;
        return _source.MoveNext();
    }

    /// <inheritdoc />
    public void Dispose() { _source.Dispose(); }

    /// <inheritdoc />
    public TItem First()
    {
        if(!MoveNext())
            throw new InvalidOperationException();
        return Current;
    }

    /// <inheritdoc />
    public TItem? FirstOrDefault()
    {
        return MoveNext() ? Current : default;
    }

    /// <inheritdoc />
    public bool TryGetFirst(out TItem? item)
    {
        var found = MoveNext();
        item = found ? Current: default;
        return found;
    }

    /// <inheritdoc />
    public TItem Last() => _spanBased 
        ? _source.Last() 
        : Iterator.Last<RangeIterator<TIter, TItem>, TItem>(this);

    /// <inheritdoc />
    public TItem? LastOrDefault()
    {
        if (_spanBased)
            return _source.LastOrDefault();
        TryGetLast(out var last);
        return last;
    }

    /// <inheritdoc />
    public bool TryGetLast(out TItem? item) => _spanBased 
        ? _source.TryGetLast(out item) 
        : Iterator.TryGetLast(this,out item);

    /// <inheritdoc />
    public int Count()
        => _spanBased ? _source.Count() : _endExcl - _start;

    /// <inheritdoc />
    public bool TryGetNonEnumeratedCount(out int count)
    {
        count = Count();
        return true;
    }

    /// <inheritdoc />
    public void CopyTo(Span<TItem> destination)
    {
        if(_spanBased)
            _source.CopyTo(destination);
        else 
            Iterator.CopyTo(this,destination);
    }

    /// <inheritdoc />
    public RangeIterator<TIter,TItem> GetEnumerator()
    {
        var copy = this;
        copy.Reset();
        return copy;
    }

    /// <inheritdoc />
    public WhereIterator<RangeIterator<TIter,TItem>, TItem> Where(Func<TItem, bool> predicate) => new(this, predicate);
    
    
    public SelectManyIterator<RangeIterator<TIter,TItem>, TInner, TItem, TOut> SelectMany<TInner, TOut>(
        Func<TItem, TInner> flattener) where TInner : IRefIterator<TInner,TOut>, allows ref struct =>
        new(this, flattener);

    public SelectManyIterator<RangeIterator<TIter,TItem>, SpanIterator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, TOut[]> flattener) => new(this, inner => flattener(inner));

    public SelectManyIterator<RangeIterator<TIter,TItem>, SpanIterator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, List<TOut>> flattener) => new(this, inner => flattener(inner));

    public SelectManyIterator<RangeIterator<TIter,TItem>, AdapterIterator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, IEnumerable<TOut>> flattener) => new(this, Func.Combine(flattener,Iterator.Adapt));

    /// <inheritdoc />
    public SelectIterator<RangeIterator<TIter,TItem>, TItem, TOut> Select<TOut>(Func<TItem, TOut> selector)
        => new(this, selector);

    /// <inheritdoc />
    RangeIterator<RangeIterator<TIter, TItem>, TItem> IRefIterator<RangeIterator<TIter, TItem>, TItem>.Take(Range r) => new(this, r, Count());


    /// <inheritdoc />
    RangeIterator<RangeIterator<TIter, TItem>, TItem> IRefIterator<RangeIterator<TIter, TItem>, TItem>.Take(int num)
        => new(this, ..num, Count());


    /// <inheritdoc />
    RangeIterator<RangeIterator<TIter, TItem>, TItem> IRefIterator<RangeIterator<TIter, TItem>, TItem>.Skip(int num)
        => new(this, num.., Count());

    public RangeIterator<TIter, TItem> Take(Range r)
    {
        var (start,length) = r.GetOffsetAndLength(Count());
        var end = start + length;
        return new RangeIterator<TIter, TItem>(_source, start, end, _sourceCount);
    }

    public RangeIterator<TIter, TItem> Take(int num)
        => Take(..num);
    public RangeIterator<TIter, TItem> Skip(int num)
        => Take(num..);

    
    
    /// <inheritdoc />
    public TItem[] ToArray() 
        => _spanBased ? _source.ToArray() : Iterator.ToArray<RangeIterator<TIter, TItem>, TItem>(this);

    /// <inheritdoc />
    public List<TItem> ToList()
        => _spanBased ? _source.ToList() : Iterator.ToList<RangeIterator<TIter, TItem>, TItem>(this);

    /// <inheritdoc />
    public HashSet<TItem> ToHashSet()
        => ToHashSet(EqualityComparer<TItem>.Default);


    /// <inheritdoc />
    public HashSet<TItem> ToHashSet(IEqualityComparer<TItem>? comp)
        => _spanBased ? _source.ToHashSet() : Iterator.ToHashSet<RangeIterator<TIter, TItem>, TItem>(this,comp);
    
    public TItem First(Func<TItem,bool> predicate)=>Where(predicate).First();
    public TItem? FirstOrDefault(Func<TItem,bool> predicate)=>Where(predicate).FirstOrDefault();

    public TItem Last(Func<TItem,bool> predicate)=>Where(predicate).Last();
    public TItem? LastOrDefault(Func<TItem,bool> predicate)=>Where(predicate).LastOrDefault();
    
    public bool Any()
    {
        var copy = this;
        return copy.MoveNext();
    }
    public bool Any(Func<TItem,bool> predicate)=>Where(predicate).MoveNext();

    /// <inheritdoc />
    public int EstimateCount() => Count();
    public OfTypeIterator<RangeIterator<TIter,TItem>,TItem, TOther> OfType<TOther>() => new(this);
    
    
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
        ToDictionary(keyGen, valGen,null);

    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
        Func<TItem, TKey> keyGen, 
        Func<TItem, TValue> valGen,
        IEqualityComparer<TKey>? comp) 
        where TKey : notnull =>
        Iterator.ToDictionary(this,comp,keyGen, valGen);

    public Dictionary<TKey, TItem> ToDictionary<TKey>(
        Func<TItem, TKey> keyGen,
        IEqualityComparer<TKey>? comp)
        where TKey : notnull
        => ToDictionary(keyGen, x => x, comp);
    public Dictionary<TKey, TItem> ToDictionary<TKey>(
        Func<TItem, TKey> keyGen)
        where TKey : notnull
        => ToDictionary(keyGen, x => x, null);
    
    public bool All(Func<TItem,bool> predicate)=>!Any(x=>!predicate(x));
    public bool TryTakeRange(Range r, out RangeIterator<TIter, TItem> result)
    {
        result = Take(r);
        return true;
    }
    
    
    public DistinctIterator<RangeIterator<TIter,TItem>, TItem> Distinct()
        => Distinct(null);
    public DistinctIterator<RangeIterator<TIter,TItem>, TItem> Distinct(IEqualityComparer<TItem>? comp)
        => new(this, comp);
    public DistinctIterator<RangeIterator<TIter,TItem>,TItem> DistinctBy<T>(Func<TItem, T> keySelector) where T : notnull 
        =>new(this,Equality.By(keySelector));

    public ConcatIterator<RangeIterator<TIter,TItem>, TOther, TItem> Concat<TOther>(TOther other) 
        where TOther : IRefIterator<TOther, TItem>,allows ref struct
        => new(this, other);
    public ConcatIterator<RangeIterator<TIter,TItem>, ItemIterator<TItem>, TItem> Append(TItem append) 
        => new(this, append);
    public ConcatIterator<ItemIterator<TItem>,RangeIterator<TIter,TItem>, TItem> Prepend(TItem prepend) 
        => new(prepend,this);

    public static RangeIterator<TIter, TItem> Empty() => new(TIter.Empty(), Range.All, 0);
    
    
    /// <inheritdoc />
    public TItem Min()
        => Min(null);

    /// <inheritdoc />
    public TItem Max()
        => Max(null);

    /// <inheritdoc />
    public TItem? MinOrDefault()
        => MinOrDefault(null);

    /// <inheritdoc />
    public TItem? MaxOrDefault()
        => MaxOrDefault(null);

    /// <inheritdoc />
    public TItem Min(IComparer<TItem>? comp)
        =>Iterator.TryGetMin(this,comp,out var min)?min!:ThrowHelper.ThrowInvalidOperationException<TItem>();

    /// <inheritdoc />
    public TItem Max(IComparer<TItem>? comp)
        =>Iterator.TryGetMax(this,comp,out var min)?min!:ThrowHelper.ThrowInvalidOperationException<TItem>();

    /// <inheritdoc />
    public TItem? MinOrDefault(IComparer<TItem>? comp)
    {
        Iterator.TryGetMin(this,comp,out var min);
        return min;
    }

    /// <inheritdoc />
    public TItem? MaxOrDefault(IComparer<TItem>? comp)
    {
        Iterator.TryGetMax(this,comp,out var min);
        return min;
    }

    /// <inheritdoc />
    public TItem MinBy<TKey>(Func<TItem, TKey> bySel) where TKey : IComparable<TKey> => Min(Equality.CompareBy(bySel));

    /// <inheritdoc />
    public TItem MaxBy<TKey>(Func<TItem, TKey> bySel) where TKey : IComparable<TKey> => Max(Equality.CompareBy(bySel));

    /// <inheritdoc />
    public TItem? MinOrDefaultBy<TKey>(Func<TItem, TKey> bySel) where TKey : IComparable<TKey>
        => MinOrDefault(Equality.CompareBy(bySel));

    /// <inheritdoc />
    public TItem? MaxOrDefaultBy<TKey>(Func<TItem, TKey> bySel) where TKey : IComparable<TKey>
        => MaxOrDefault(Equality.CompareBy(bySel));
}