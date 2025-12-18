using System.Collections;

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
    public TItem Current => _current!;

    /// <inheritdoc />
    object? IEnumerator.Current => _current;

    /// <inheritdoc />
    public void Dispose() { }


    /// <inheritdoc />
    public TItem First() => Iterator.First<FilterIterator<TIter, TItem>, TItem>(this);

    /// <inheritdoc />
    public TItem? FirstOrDefault()
    {
        Iterator.TryGetFirst<FilterIterator<TIter, TItem>, TItem>(this, out var item);
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
    public FilterIterator<FilterIterator<TIter, TItem>, TItem> Where(Func<TItem, bool> predicate) => new(this, predicate);

    /// <inheritdoc />
    public SelectIterator<FilterIterator<TIter, TItem>, TItem, TOut> Select<TOut>(Func<TItem, TOut> selector) => new(this,selector);

    /// <inheritdoc />
    public RangeIterator<FilterIterator<TIter,TItem>, TItem> Take(Range r) 
        => Iterator.Take<FilterIterator<TIter,TItem>, TItem>(this, r);

    /// <inheritdoc />
    public RangeIterator<FilterIterator<TIter,TItem>, TItem> Take(int num)
        => Iterator.Take<FilterIterator<TIter,TItem>, TItem>(this, num);
    

    /// <inheritdoc />
    public RangeIterator<FilterIterator<TIter,TItem>, TItem> Skip(int num)
        => Iterator.Skip<FilterIterator<TIter,TItem>, TItem>(this, num);
    
    
    /// <inheritdoc />
    public TItem[] ToArray() => Iterator.ToArray<FilterIterator<TIter, TItem>, TItem>(this);

    /// <inheritdoc />
    public List<TItem> ToList() => Iterator.ToList<FilterIterator<TIter, TItem>, TItem>(this);

    /// <inheritdoc />
    public HashSet<TItem> ToHashSet() => Iterator.ToHashSet<FilterIterator<TIter, TItem>, TItem>(this);
    public HashSet<TItem> ToHashSet(IEqualityComparer<TItem> comp)
        =>Iterator.ToHashSet(this,comp);
    
    public TItem First(Func<TItem,bool> predicate)=>this.Where(predicate).First();
    public TItem? FirstOrDefault(Func<TItem,bool> predicate)=>this.Where(predicate).FirstOrDefault();

    public TItem Last(Func<TItem,bool> predicate)=>this.Where(predicate).Last();
    public TItem? LastOrDefault(Func<TItem,bool> predicate)=>this.Where(predicate).LastOrDefault();
    
    
    public bool Any()
    {
        var copy = this;
        copy.Reset();
        return copy.MoveNext();
    }
    public bool Any(Func<TItem,bool> predicate)=>Where(predicate).MoveNext();

    /// <inheritdoc />
    public int MaxPossibleCount()
        => _source.MaxPossibleCount();
    public OfTypeIterator<FilterIterator<TIter, TItem>,TItem, TOther> OfType<TOther>() => new(this);
    
}