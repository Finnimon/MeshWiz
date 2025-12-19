using System.Collections;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.RefLinq;

public ref struct SpanIterator<TItem>(ReadOnlySpan<TItem> source) : IRefIterator<SpanIterator<TItem>, TItem>
{
    private int _position = -1;
    private readonly ReadOnlySpan<TItem> _source = source;

    /// <inheritdoc />
    public bool MoveNext() => _source.Length > (uint)++_position;

    /// <inheritdoc />
    public void Reset() => _position = -1;

    /// <inheritdoc />
    readonly object? IEnumerator.Current => Current;

    /// <inheritdoc />
    public readonly TItem Current => _source[_position];


    /// <inheritdoc />
    public readonly TItem First() => _source[0];

    /// <inheritdoc />
    public readonly TItem? FirstOrDefault()
        => _source.GetOrDefault(0);

    /// <inheritdoc />
    public readonly bool TryGetFirst(out TItem? item) => _source.TryGet(0, out item);

    /// <inheritdoc />
    public readonly TItem Last()
        => _source[^1];

    /// <inheritdoc />
    public readonly TItem? LastOrDefault() => _source.GetOrDefault(^1);

    /// <inheritdoc />
    public readonly bool TryGetLast(out TItem? item) => _source.TryGet(^1, out item);

    public readonly ReadOnlySpan<TItem> OriginalSource => _source;

    /// <inheritdoc />
    public readonly int Count() => _source.Length;

    /// <inheritdoc />
    public readonly bool TryGetNonEnumeratedCount(out int count)
    {
        count = _source.Length;
        return true;
    }

    /// <inheritdoc />
    public readonly void CopyTo(Span<TItem> destination)
    {
        _source.CopyTo(destination);
    }

    /// <inheritdoc />
    public readonly void Dispose() { }

    public readonly SpanIterator<TItem> GetEnumerator()
    {
        var copy = this;
        copy.Reset();
        return copy;
    }

    /// <inheritdoc />
    public FilterIterator<SpanIterator<TItem>, TItem> Where(Func<TItem, bool> predicate) => new(this, predicate);

    /// <inheritdoc />
    public SelectIterator<SpanIterator<TItem>, TItem, TOut> Select<TOut>(Func<TItem, TOut> selector) => new(this, selector);
    /// <inheritdoc />
    public RangeIterator<SpanIterator<TItem>, TItem> Take(Range r) 
        => Iterator.Take<SpanIterator<TItem>, TItem>(this, r);

    /// <inheritdoc />
    public RangeIterator<SpanIterator<TItem>, TItem> Take(int num)
        => Iterator.Take<SpanIterator<TItem>, TItem>(this, num);
    

    /// <inheritdoc />
    public RangeIterator<SpanIterator<TItem>, TItem> Skip(int num)
        => Iterator.Skip<SpanIterator<TItem>, TItem>(this, num);
    
    public static implicit operator SpanIterator<TItem>(ReadOnlySpan<TItem> span)
        => new(span);
    public static implicit operator SpanIterator<TItem>(List<TItem> data) => new(CollectionsMarshal.AsSpan(data));
    
    public TItem[] ToArray() => _source.ToArray();

    /// <inheritdoc />
    public List<TItem> ToList()
    {
        List<TItem> l = new(_source.Length);
        l.AddRange(_source);
        return l;
    }

    /// <inheritdoc />
    public HashSet<TItem> ToHashSet() => [..ToArray()];
    /// <inheritdoc />
    public HashSet<TItem> ToHashSet(IEqualityComparer<TItem> comp) => new(ToArray(),comp);
    
    public TItem First(Func<TItem,bool> predicate)=>this.Where(predicate).First();
    public TItem? FirstOrDefault(Func<TItem,bool> predicate)=>this.Where(predicate).FirstOrDefault();

    public TItem Last(Func<TItem,bool> predicate)=>this.Where(predicate).Last();
    public TItem? LastOrDefault(Func<TItem,bool> predicate)=>this.Where(predicate).LastOrDefault();
    
    public bool Any()
    {
        var copy = this;
        return copy.MoveNext();
    }
    public bool Any(Func<TItem,bool> predicate)=>Where(predicate).MoveNext();

    /// <inheritdoc />
    public int MaxPossibleCount() => Count();
    public OfTypeIterator<SpanIterator<TItem>,TItem, TOther> OfType<TOther>() => new(this);
    
    
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
}