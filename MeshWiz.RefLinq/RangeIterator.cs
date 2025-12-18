using System.Collections;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.RefLinq;

public ref struct RangeIterator<TIter, TItem> : IRefIterator<RangeIterator<TIter,TItem>,TItem>
    where TIter : IRefIterator<TIter, TItem>, allows ref struct

{
    private TIter _source;
    private readonly int _start, _endExcl;
    private int _pos;
    private readonly bool _spanBased;

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
            return;
        }

        _source = source;
        var sourceCount = _source.Count();
        _start= range.Start.GetOffset(sourceCount);
        _endExcl = range.End.GetOffset(sourceCount);
        var count = _endExcl - _start;
        if (count < 0||_start<0)
            ThrowHelper.ThrowInvalidOperationException();
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
    public FilterIterator<RangeIterator<TIter,TItem>, TItem> Where(Func<TItem, bool> predicate) => new(this, predicate);

    /// <inheritdoc />
    public SelectIterator<RangeIterator<TIter,TItem>, TItem, TOut> Select<TOut>(Func<TItem, TOut> selector)
        => new(this, selector);

    /// <inheritdoc />
    public RangeIterator<RangeIterator<TIter, TItem>, TItem> Take(Range r) 
        => Iterator.Take<RangeIterator<TIter, TItem>, TItem>(this, r);

    /// <inheritdoc />
    public RangeIterator<RangeIterator<TIter, TItem>, TItem> Take(int num)
        => Iterator.Take<RangeIterator<TIter, TItem>, TItem>(this, num);
    

    /// <inheritdoc />
    public RangeIterator<RangeIterator<TIter, TItem>, TItem> Skip(int num)
        => Iterator.Skip<RangeIterator<TIter, TItem>, TItem>(this, num);

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
    public HashSet<TItem> ToHashSet(IEqualityComparer<TItem> comp)
        => _spanBased ? _source.ToHashSet() : Iterator.ToHashSet<RangeIterator<TIter, TItem>, TItem>(this,comp);
    
}