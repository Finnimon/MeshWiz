using System.Collections;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;

namespace MeshWiz.RefLinq;

public ref struct SmartSelectIterator<TIn, TOut>(ReadOnlySpan<TIn> source,Func<TIn,TOut> sel):IRefIterator<SmartSelectIterator<TIn,TOut>,TOut>
{
    public readonly ReadOnlySpan<TIn> Source=source;
    private readonly Func<TIn, TOut> _sel=sel;
    private int _index=-1;
    public readonly int Length => Source.Length;

    /// <inheritdoc />
    public bool MoveNext()
        => Source.Length>(uint)++_index;

    /// <inheritdoc />
    public void Reset() => _index = -1;

    /// <inheritdoc />
    object? IEnumerator.Current => Current;

    public TOut Current => _sel(Source[_index]);
    public TOut this[int index]
    {
        get
        {
            if(Length<(uint)index) IndexThrowHelper.Throw(index,Length);
            return _sel(Source[index]);
        }
    }

    public SmartSelectIterator<TIn, TOut> this[Range r] => new(Source[r], _sel);

    /// <inheritdoc />
    public void Dispose() { }

    /// <inheritdoc />
    public TOut First() => this[0];

    /// <inheritdoc />
    public TOut? FirstOrDefault()
    {
        TryGetFirst(out var first);
        return first;
    }

    /// <inheritdoc />
    public bool TryGetFirst(out TOut? item)
    {
        if (Length == 0)
        {
            item = default;
            return false;
        }
        item = this[0];
        return true;
    }

    /// <inheritdoc />
    public TOut Last()
        => this[^1];

    /// <inheritdoc />
    public TOut? LastOrDefault()
    {
        TryGetLast(out var last);
        return last;
    }

    /// <inheritdoc />
    public bool TryGetLast(out TOut? item)
    {
        if (Length == 0)
        {
            item = default;
            return false;
        }
        item = this[^1];
        return true;
    }

    /// <inheritdoc />
    int IRefIterator<SmartSelectIterator<TIn, TOut>, TOut>.Count()
        => Length;

    /// <inheritdoc />
    bool IRefIterator<SmartSelectIterator<TIn, TOut>, TOut>.TryGetNonEnumeratedCount(out int count)
    {
        count = Length;
        return true;
    }

    /// <inheritdoc />
    public void CopyTo(Span<TOut> destination)
    {
        for (var i = 0; i < Length; i++) destination[i] = this[i];
    }

    /// <inheritdoc />
    public SmartSelectIterator<TIn, TOut> GetEnumerator()
    {
        var copy = this;
        copy.Reset();
        return copy;
    }

    /// <inheritdoc />
    public WhereIterator<SmartSelectIterator<TIn, TOut>, TOut> Where(Func<TOut, bool> predicate) => new(this, predicate);
    

    /// <inheritdoc />
    SelectIterator<SmartSelectIterator<TIn, TOut>, TOut, TOut1> IRefIterator<SmartSelectIterator<TIn, TOut>, TOut>.Select<TOut1>(Func<TOut, TOut1> selector) 
        => new(this, selector);

    public SmartSelectIterator<TIn, TOut2> Select<TOut2>(Func<TOut, TOut2> sel2) => new(Source, Func.Combine(_sel, sel2));

    public SmartSelectIterator<TIn, TOut> Take(Range r) => this[r];
    public SmartSelectIterator<TIn, TOut> Take(int num) => this[..num];
    public SmartSelectIterator<TIn, TOut> Skip(int num) => this[num..];
    /// <inheritdoc />
    RangeIterator<SmartSelectIterator<TIn, TOut>, TOut> IRefIterator<SmartSelectIterator<TIn, TOut>, TOut>.Take(Range r) => new(this, r);

    /// <inheritdoc />
    RangeIterator<SmartSelectIterator<TIn, TOut>, TOut> IRefIterator<SmartSelectIterator<TIn, TOut>, TOut>.Take(int num) => new(this, ..num);

    /// <inheritdoc />
    RangeIterator<SmartSelectIterator<TIn, TOut>, TOut> IRefIterator<SmartSelectIterator<TIn, TOut>, TOut>.Skip(int num) => new(this, num..);

    /// <inheritdoc />
    public TOut[] ToArray() => Iterator.ToArray<SmartSelectIterator<TIn, TOut>, TOut>(this);

    /// <inheritdoc />
    public List<TOut> ToList()
        => Iterator.ToList<SmartSelectIterator<TIn, TOut>, TOut>(this);

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
    int IRefIterator<SmartSelectIterator<TIn, TOut>, TOut>.EstimateCount()
        => Length;

    /// <inheritdoc />
    public OfTypeIterator<SmartSelectIterator<TIn, TOut>, TOut, TOther> OfType<TOther>()
        => new(this);

    /// <inheritdoc />
    public TOut Aggregate(Func<TOut, TOut, TOut> aggregator)
        => Iterator.Aggregate(this, aggregator);

    /// <inheritdoc />
    public TOther Aggregate<TOther>(Func<TOther, TOut, TOther> aggregator, TOther seed)
        => Iterator.Aggregate(this, aggregator,seed);

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
    public SelectManyIterator<SmartSelectIterator<TIn, TOut>, TInner, TOut, TOut1> SelectMany<TInner, TOut1>(Func<TOut, TInner> flattener) where TInner : IRefIterator<TInner,TOut1>, allows ref struct 
        => new(this, flattener);

    /// <inheritdoc />
    public SelectManyIterator<SmartSelectIterator<TIn, TOut>, SpanIterator<TOut1>, TOut, TOut1> SelectMany<TOut1>(
        Func<TOut, TOut1[]> flattener)
        => new(this,x => flattener(x));

    /// <inheritdoc />
    public SelectManyIterator<SmartSelectIterator<TIn, TOut>, SpanIterator<TOut1>, TOut, TOut1> SelectMany<TOut1>(Func<TOut, List<TOut1>> flattener)
        => new(this,x => flattener(x));
    
    public SelectManyIterator<SmartSelectIterator<TIn,TOut>, SpanIterator<TOut2>, TOut, TOut2> SelectMany<TOut2>(
        Func<TOut, SpanIterator<TOut2>> flattener)
        => new(this, flattener);

    /// <inheritdoc />
    public SelectManyIterator<SmartSelectIterator<TIn, TOut>, AdapterIterator<TOut1>, TOut, TOut1> SelectMany<TOut1>(Func<TOut, IEnumerable<TOut1>> flattener)
        => new(this,Func.Combine(flattener,Iterator.Adapt));

    /// <inheritdoc />
    public bool TryTakeRange(Range r, out SmartSelectIterator<TIn, TOut> result)
    {
        result = this[r];
        return true;
    }
    
    
    public DistinctIterator<SmartSelectIterator<TIn,TOut>, TOut> Distinct()
        => Distinct(null);
    public DistinctIterator<SmartSelectIterator<TIn,TOut>, TOut> Distinct(IEqualityComparer<TOut>? comp)
        => new(this, comp);
    public DistinctIterator<SmartSelectIterator<TIn,TOut>,TOut> DistinctBy<T>(Func<TOut, T> keySelector) where T : notnull 
        =>new(this,Equality.By(keySelector));

    public ConcatIterator<SmartSelectIterator<TIn,TOut>, TOther, TOut> Concat<TOther>(TOther other) 
        where TOther : IRefIterator<TOther, TOut>,allows ref struct
        => new(this, other);
    public ConcatIterator<SmartSelectIterator<TIn,TOut>, ItemIterator<TOut>, TOut> Append(TOut append) 
        => new(this, append);
    public ConcatIterator<ItemIterator<TOut>,SmartSelectIterator<TIn,TOut>, TOut> Prepend(TOut prepend) 
        => new(prepend,this);

    public static SmartSelectIterator<TIn, TOut> Empty() => new(ReadOnlySpan<TIn>.Empty, x => (TOut)(object)x!);
    
    
    
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