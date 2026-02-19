using System.Collections;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;

namespace MeshWiz.RefLinq;

public ref struct SmartSelectWhereIterator<TIn, TOut>(ReadOnlySpan<TIn> source,Func<TIn,TOut> sel, Func<TOut,bool> filter):IRefIterator<SmartSelectWhereIterator<TIn,TOut>,TOut>
{
    public readonly ReadOnlySpan<TIn> Source=source;
    private readonly Func<TIn, TOut> _sel=sel;
    private readonly Func<TOut, bool> _filter=filter;
    private int _index=-1;
    private TOut? _current;
    public readonly int Length => Source.Length;

    /// <inheritdoc />
    public bool MoveNext()
    {
        var filter = _filter;
        var sel = _sel;
        while(Source.Length > (uint)++_index)
        {
            _current=sel(Source[_index]);
            if (filter(_current)) return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void Reset() => _index = -1;

    /// <inheritdoc />
    readonly object? IEnumerator.Current => Current;

    public readonly TOut Current => _current!;
    public readonly TOut this[int index]
    {
        get
        {
            if(Length<(uint)index) IndexThrowHelper.Throw(index,Length);
            return _sel(Source[index]);
        }
    }


    /// <inheritdoc />
    public readonly void Dispose() { }

    /// <inheritdoc />
    public readonly  TOut First() => Iterator.First<SmartSelectWhereIterator<TIn, TOut>, TOut>(this);

    /// <inheritdoc />
    public TOut? FirstOrDefault()
    {
        TryGetFirst(out var first);
        return first;
    }

    /// <inheritdoc />
    public bool TryGetFirst(out TOut? item)
    {
        var ret = MoveNext();
        item = _current;
        return ret;
    }

    /// <inheritdoc />
    public TOut Last()
        => Iterator.Last<SmartSelectWhereIterator<TIn, TOut>, TOut>(this);

    /// <inheritdoc />
    public TOut? LastOrDefault()
    {
        TryGetLast(out var last);
        return last;
    }

    /// <inheritdoc />
    public bool TryGetLast(out TOut? item)
    {
        var src = Source;
        var sel = _sel;
        var filter=_filter;
        for (var i = src.Length - 1; i >= 0; i--)
        {
            var cur = sel(src[i]);
            if(!filter(cur)) continue;
            item = cur;
            return true;
        }
        Unsafe.SkipInit(out item);
        return false;
    }

    /// <inheritdoc />
    public int Count() => Iterator.Count<SmartSelectWhereIterator<TIn, TOut>, TOut>(this);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetNonEnumeratedCount(out int count)
    {
        count = 0;
        return Length==0;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(Span<TOut> destination) => Iterator.CopyTo(this, destination);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(TOut[] array, int arrayIndex) => CopyTo(array.AsSpan(arrayIndex));

    /// <inheritdoc />
    public SmartSelectWhereIterator<TIn, TOut> GetEnumerator()
    {
        var copy = this;
        copy.Reset();
        return copy;
    }

    /// <inheritdoc />
    public WhereIterator<SmartSelectWhereIterator<TIn, TOut>, TOut> Where(Func<TOut, bool> predicate) => new(this, predicate);
    

    /// <inheritdoc />
    SelectIterator<SmartSelectWhereIterator<TIn, TOut>, TOut, TOut1> IRefIterator<SmartSelectWhereIterator<TIn, TOut>, TOut>.Select<TOut1>(Func<TOut, TOut1> selector) 
        => new(this, selector);
    /// <inheritdoc />
    RangedIterator<SmartSelectWhereIterator<TIn, TOut>, TOut> IRefIterator<SmartSelectWhereIterator<TIn, TOut>, TOut>.Take(Range r) => new(this, r);

    /// <inheritdoc />
    RangedIterator<SmartSelectWhereIterator<TIn, TOut>, TOut> IRefIterator<SmartSelectWhereIterator<TIn, TOut>, TOut>.Take(int num) => new(this, ..num);

    /// <inheritdoc />
    RangedIterator<SmartSelectWhereIterator<TIn, TOut>, TOut> IRefIterator<SmartSelectWhereIterator<TIn, TOut>, TOut>.Skip(int num) => new(this, num..);

    /// <inheritdoc />
    public TOut[] ToArray() => Iterator.ToArray<SmartSelectWhereIterator<TIn, TOut>, TOut>(this);

    /// <inheritdoc />
    public List<TOut> ToList()
        => Iterator.ToList<SmartSelectWhereIterator<TIn, TOut>, TOut>(this);

    /// <inheritdoc />
    public HashSet<TOut> ToHashSet()
        => ToHashSet(null);

    /// <inheritdoc />
    public HashSet<TOut> ToHashSet(IEqualityComparer<TOut>? comp) => Iterator.ToHashSet(this,comp);

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
    public bool Any() => Length > 0;

    /// <inheritdoc />
    public bool Any(Func<TOut, bool> predicate)
        => Where(predicate).Any();

    /// <inheritdoc />
    public bool All(Func<TOut, bool> predicate)
        => !Any(Func.Invert(predicate));

    /// <inheritdoc />
    int IRefIterator<SmartSelectWhereIterator<TIn, TOut>, TOut>.EstimateCount()
        => Length;

    /// <inheritdoc />
    public OfTypeIterator<SmartSelectWhereIterator<TIn, TOut>, TOut, TOther> OfType<TOther>()
        => new(this);

    /// <inheritdoc />
    public TOut Aggregate(Func<TOut, TOut, TOut> aggregator)
    {
        switch (Length)
        {
            case 0:
                return ThrowHelper.ThrowInvalidOperationException<TOut>();
            case 1:
                return this[0];
            case 2:
                return aggregator(this[0], this[1]);
            default:
                var seed = this[0];
                for (var i = 1; i < Length; i++)
                    seed = aggregator(seed, this[i]);
                return seed;
        }
    }

    /// <inheritdoc />
    public TOther Aggregate<TOther>(Func<TOther, TOut, TOther> aggregator, TOther seed)
    {
        switch (Length)
        {
            case 0:
                return seed;
            case 1:
                return aggregator(seed, this[0]);
            case 2:
                return aggregator(aggregator(seed, this[0]), this[1]);
            default:
                for (var i=0;i<Length;i++)
                    seed = aggregator(seed, this[i]);
                return seed;
        }
    }

    /// <inheritdoc />
    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(Func<TOut, TKey> keyGen, Func<TOut, TValue> valGen)
        where TKey : notnull
        => ToDictionary(keyGen, valGen,null);

    /// <inheritdoc />
    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(Func<TOut, TKey> keyGen, Func<TOut, TValue> valGen, IEqualityComparer<TKey>? comp) where TKey : notnull
        =>Iterator.ToDictionary(this,comp,keyGen,valGen);

    /// <inheritdoc />
    public Dictionary<TKey, TOut> ToDictionary<TKey>(Func<TOut, TKey> keyGen, IEqualityComparer<TKey>? comp) where TKey : notnull
        =>ToDictionary(keyGen, Func.Identity, comp);

    /// <inheritdoc />
    public Dictionary<TKey, TOut> ToDictionary<TKey>(Func<TOut, TKey> keyGen) where TKey : notnull => ToDictionary(keyGen, Func.Identity);

    /// <inheritdoc />
    public SelectManyIterator<SmartSelectWhereIterator<TIn, TOut>, TInner, TOut, TOut1> SelectMany<TInner, TOut1>(Func<TOut, TInner> flattener) where TInner : IRefIterator<TInner,TOut1>, allows ref struct 
        => new(this, flattener);

    /// <inheritdoc />
    public SelectManyIterator<SmartSelectWhereIterator<TIn, TOut>, SpanIterator<TOut1>, TOut, TOut1> SelectMany<TOut1>(
        Func<TOut, TOut1[]> flattener)
        => new(this,x => flattener(x));

    /// <inheritdoc />
    public SelectManyIterator<SmartSelectWhereIterator<TIn, TOut>, SpanIterator<TOut1>, TOut, TOut1> SelectMany<TOut1>(Func<TOut, List<TOut1>> flattener)
        => new(this,x => flattener(x));
    
    public SelectManyIterator<SmartSelectWhereIterator<TIn,TOut>, SpanIterator<TOut2>, TOut, TOut2> SelectMany<TOut2>(
        Func<TOut, SpanIterator<TOut2>> flattener)
        => new(this, flattener);

    /// <inheritdoc />
    public SelectManyIterator<SmartSelectWhereIterator<TIn, TOut>, Iterator<TOut1>, TOut, TOut1> SelectMany<TOut1>(Func<TOut, IEnumerable<TOut1>> flattener)
        => new(this,Func.Combine(flattener,Iterator.Iterate));

    /// <inheritdoc />
    bool IRefIterator<SmartSelectWhereIterator<TIn, TOut>, TOut>.TryTakeRange(Range r, out SmartSelectWhereIterator<TIn, TOut> result)
    {
        Unsafe.SkipInit(out result);
        return false;
    }
    
    
    public DistinctIterator<SmartSelectWhereIterator<TIn,TOut>, TOut> Distinct()
        => Distinct(null);
    public DistinctIterator<SmartSelectWhereIterator<TIn,TOut>, TOut> Distinct(IEqualityComparer<TOut>? comp)
        => new(this, comp);
    public DistinctIterator<SmartSelectWhereIterator<TIn,TOut>,TOut> DistinctBy<T>(Func<TOut, T> keySelector) where T : notnull 
        =>new(this,Equality.By(keySelector));

    public ConcatIterator<SmartSelectWhereIterator<TIn,TOut>, TOther, TOut> Concat<TOther>(TOther other) 
        where TOther : IRefIterator<TOther, TOut>,allows ref struct
        => new(this, other);
    public ConcatIterator<SmartSelectWhereIterator<TIn,TOut>, ItemIterator<TOut>, TOut> Append(TOut append) 
        => new(this, append);
    public ConcatIterator<ItemIterator<TOut>,SmartSelectWhereIterator<TIn,TOut>, TOut> Prepend(TOut prepend) 
        => new(prepend,this);

    public static SmartSelectWhereIterator<TIn, TOut> Empty() => new(ReadOnlySpan<TIn>.Empty, x => (TOut)(object)x!,x=>true);
    
    
    
    /// <inheritdoc />
    public TOut Min()
        => Min(null);

    /// <inheritdoc />
    public TOut Max()
        => Max(null);

    /// <inheritdoc />
    public TOut? MinOrDefault()
        => MinOrDefault(null);

    /// <inheritdoc />
    public TOut? MaxOrDefault()
        => MaxOrDefault(null);

    /// <inheritdoc />
    public TOut Min(IComparer<TOut>? comp)
        =>Iterator.TryGetMin(this,comp,out var min)?min!:ThrowHelper.ThrowInvalidOperationException<TOut>();

    /// <inheritdoc />
    public TOut Max(IComparer<TOut>? comp)
        =>Iterator.TryGetMax(this,comp,out var min)?min!:ThrowHelper.ThrowInvalidOperationException<TOut>();

    /// <inheritdoc />
    public TOut? MinOrDefault(IComparer<TOut>? comp)
    {
        Iterator.TryGetMin(this,comp,out var min);
        return min;
    }

    /// <inheritdoc />
    public TOut? MaxOrDefault(IComparer<TOut>? comp)
    {
        Iterator.TryGetMax(this,comp,out var min);
        return min;
    }

    /// <inheritdoc />
    public TOut MinBy<TKey>(Func<TOut, TKey> bySel) where TKey : IComparable<TKey> => Min(Equality.CompareBy(bySel));

    /// <inheritdoc />
    public TOut MaxBy<TKey>(Func<TOut, TKey> bySel) where TKey : IComparable<TKey> => Max(Equality.CompareBy(bySel));

    /// <inheritdoc />
    public TOut? MinOrDefaultBy<TKey>(Func<TOut, TKey> bySel) where TKey : IComparable<TKey>
        => MinOrDefault(Equality.CompareBy(bySel));

    /// <inheritdoc />
    public TOut? MaxOrDefaultBy<TKey>(Func<TOut, TKey> bySel) where TKey : IComparable<TKey>
        => MaxOrDefault(Equality.CompareBy(bySel));
}

