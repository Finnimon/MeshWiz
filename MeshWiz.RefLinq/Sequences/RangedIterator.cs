using System.Collections;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;

namespace MeshWiz.RefLinq;

public ref struct RangedIterator<TIter, TItem> : IRefIterator<RangedIterator<TIter, TItem>, TItem>
    where TIter : IRefIterator<TIter, TItem>, allows ref struct

{
    internal TIter _source;
    internal readonly int _start, _endExcl;
    private int _pos;
    private readonly bool _innerRanged;
    private readonly int _sourceCount;

    private RangedIterator(TIter source, Range r, int sourceCount)
    {
        _pos = -1;
        _sourceCount = sourceCount;
        if (source.TryConvertToSpanIter<TIter, TItem>(out var spanIterator))
        {
            spanIterator = spanIterator.OriginalSource[r];
            _source = Unsafe.As<SpanIterator<TItem>, TIter>(ref spanIterator);
            _innerRanged = true;
            _start = 0;
            _endExcl = spanIterator.OriginalSource.Length;
            return;
        }

        _source = source;
        _source.Reset();
        _start = r.Start.GetOffset(sourceCount);
        _endExcl = r.End.GetOffset(sourceCount);
        var count = _endExcl - _start;
        if (count < 0 || _start < 0)
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

    public RangedIterator(TIter source, Range range)
    {
        _pos = -1;
        if (source.TryTakeRange(range, out var rangedIter))
        {
            _source = rangedIter!;
            _innerRanged = true;
            _sourceCount = range.Start.IsFromEnd == range.End.IsFromEnd
                ? int.Abs(range.Start.Value - range.End.Value)
                : _source.Count();
            _start = 0;
            _endExcl = _sourceCount;
            return;
        }

        _source = source;
        var sourceCount = _source.Count();
        _sourceCount = sourceCount;
        _source.Reset();
        _start = range.Start.GetOffset(sourceCount);
        _endExcl = range.End.GetOffset(sourceCount);
        var count = _endExcl - _start;
        if (count < 0 || _start < 0)
            ThrowHelper.ThrowInvalidOperationException();
    }

    private RangedIterator(TIter source, int start, int end, int sourceCount)
    {
        _pos = -1;
        if (source.TryTakeRange(start..end, out var ranged))
        {
            _source = ranged;
            _start = 0;
            _endExcl = end;
            _sourceCount = _start - _endExcl;
            _innerRanged = true;
            return;
        }

        _innerRanged = false;
        _source = source;
        _start = start;
        _endExcl = end;
        _sourceCount = sourceCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => _innerRanged ? _source.MoveNext() : MoveNextSlow();

    private bool MoveNextSlow()
    {
        while (MoveSource())
            if (_pos >= _start && _pos < _endExcl)
                return true;

        return false;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool MoveSource()
    {
        _pos++;
        return _source.MoveNext();
    }

    /// <inheritdoc />
    public void Dispose() => _source.Dispose();

    /// <inheritdoc />
    public TItem First()
    {
        if(_innerRanged)
            return _source.First();
        if (!MoveNext())
            throw new InvalidOperationException();
        return Current;
    }

    /// <inheritdoc />
    public TItem? FirstOrDefault()
    {
        if(_innerRanged)
            return _source.FirstOrDefault();
        return MoveNext() ? Current : default;
    }

    /// <inheritdoc />
    public bool TryGetFirst(out TItem? item)
    {
        if(_innerRanged)
            return _source.TryGetFirst(out item);
        var found = MoveNext();
        item = found ? Current : default;
        return found;
    }

    /// <inheritdoc />
    public TItem Last() => _innerRanged
        ? _source.Last()
        : Iterator.Last<RangedIterator<TIter, TItem>, TItem>(this);

    /// <inheritdoc />
    public TItem? LastOrDefault()
    {
        if (_innerRanged)
            return _source.LastOrDefault();
        TryGetLast(out var last);
        return last;
    }

    /// <inheritdoc />
    public bool TryGetLast(out TItem? item) => _innerRanged
        ? _source.TryGetLast(out item)
        : Iterator.TryGetLast(this, out item);

    /// <inheritdoc />
    public int Count()
        => _endExcl - _start;

    /// <inheritdoc />
    public bool TryGetNonEnumeratedCount(out int count)
    {
        count = Count();
        return true;
    }

    /// <inheritdoc />
    public void CopyTo(Span<TItem> destination)
    {
        if (_innerRanged)
            _source.CopyTo(destination);
        else
            Iterator.CopyTo(this, destination);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(TItem[] array, int arrayIndex)=>CopyTo(array.AsSpan(arrayIndex));


    /// <inheritdoc />
    public RangedIterator<TIter, TItem> GetEnumerator()
    {
        var copy = this;
        copy.Reset();
        return copy;
    }

    /// <inheritdoc />
    public WhereIterator<RangedIterator<TIter, TItem>, TItem> Where(Func<TItem, bool> predicate) => new(this, predicate);


    public SelectManyIterator<RangedIterator<TIter, TItem>, TInner, TItem, TOut> SelectMany<TInner, TOut>(
        Func<TItem, TInner> flattener) where TInner : IRefIterator<TInner, TOut>, allows ref struct =>
        new(this, flattener);

    public SelectManyIterator<RangedIterator<TIter, TItem>, SpanIterator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, TOut[]> flattener) => new(this, inner => flattener(inner));

    public SelectManyIterator<RangedIterator<TIter, TItem>, SpanIterator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, List<TOut>> flattener) => new(this, inner => flattener(inner));

    public SelectManyIterator<RangedIterator<TIter, TItem>, Iterator<TOut>, TItem, TOut> SelectMany<TOut>(
        Func<TItem, IEnumerable<TOut>> flattener) => new(this, Func.Combine(flattener, Iterator.Iterate));

    /// <inheritdoc />
    public SelectIterator<RangedIterator<TIter, TItem>, TItem, TOut> Select<TOut>(Func<TItem, TOut> selector)
        => new(this, selector);

    /// <inheritdoc />
    RangedIterator<RangedIterator<TIter, TItem>, TItem> IRefIterator<RangedIterator<TIter, TItem>, TItem>.Take(Range r) =>
        new(this, r);


    /// <inheritdoc />
    RangedIterator<RangedIterator<TIter, TItem>, TItem> IRefIterator<RangedIterator<TIter, TItem>, TItem>.Take(int num)
        => new(this, ..num);


    /// <inheritdoc />
    RangedIterator<RangedIterator<TIter, TItem>, TItem> IRefIterator<RangedIterator<TIter, TItem>, TItem>.Skip(int num)
        => new(this, num..);

    public RangedIterator<TIter, TItem> Take(Range r)
    {
        var count = Count();
        var (start, length) = r.GetOffsetAndLength(count);
        start = _start + start;
        var end = start + length;
        if (start == _start && length == count)
            return this;
        return new RangedIterator<TIter, TItem>(_source, start, end, _sourceCount);
    }

    public RangedIterator<TIter, TItem> Take(int num)
        => Take(..num);

    public RangedIterator<TIter, TItem> Skip(int num)
        => Take(num..);


    /// <inheritdoc />
    public TItem[] ToArray()
        => _innerRanged ? _source.ToArray() : Iterator.ToArray<RangedIterator<TIter, TItem>, TItem>(this);

    /// <inheritdoc />
    public List<TItem> ToList()
        => _innerRanged ? _source.ToList() : Iterator.ToList<RangedIterator<TIter, TItem>, TItem>(this);

    /// <inheritdoc />
    public HashSet<TItem> ToHashSet()
        => ToHashSet(null);


    /// <inheritdoc />
    public HashSet<TItem> ToHashSet(IEqualityComparer<TItem>? comp)
        => _innerRanged ? _source.ToHashSet(comp) : Iterator.ToHashSet(this, comp);

    public TItem First(Func<TItem, bool> predicate) => _innerRanged?_source.First(predicate):Where(predicate).First();
    public TItem? FirstOrDefault(Func<TItem, bool> predicate) => _innerRanged?_source.FirstOrDefault(predicate):Where(predicate).FirstOrDefault();

    public TItem Last(Func<TItem, bool> predicate) => _innerRanged?_source.Last(predicate):Where(predicate).Last();
    public TItem? LastOrDefault(Func<TItem, bool> predicate) => _innerRanged?_source.LastOrDefault(predicate): Where(predicate).LastOrDefault();

    public bool Any()
    {
        if (_innerRanged)
            return _source.Any();
        var copy = this;
        return copy.MoveNext();
    }

    public bool Any(Func<TItem, bool> predicate) => _innerRanged ? _source.Any(predicate) : Where(predicate).Any();

    /// <inheritdoc />
    public int EstimateCount() => Count();

    public OfTypeIterator<RangedIterator<TIter, TItem>, TItem, TOther> OfType<TOther>() => new(this);


    public TItem Aggregate(Func<TItem, TItem, TItem> aggregator)
    {
        if (_innerRanged)
            return _source.Aggregate(aggregator);
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
        if (_innerRanged)
            return _source.Aggregate(aggregator, seed);
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
        _innerRanged
            ? _source.ToDictionary(keyGen, valGen, comp)
            : Iterator.ToDictionary(this, comp, keyGen, valGen);

    public Dictionary<TKey, TItem> ToDictionary<TKey>(
        Func<TItem, TKey> keyGen,
        IEqualityComparer<TKey>? comp)
        where TKey : notnull
        => ToDictionary(keyGen, Func.Identity, comp);

    public Dictionary<TKey, TItem> ToDictionary<TKey>(
        Func<TItem, TKey> keyGen)
        where TKey : notnull
        => ToDictionary(keyGen,Func.Identity, null);

    public bool All(Func<TItem, bool> predicate) => !Any(x => !predicate(x));

    public bool TryTakeRange(Range r, out RangedIterator<TIter, TItem> result)
    {
        result = Take(r);
        return true;
    }


    public DistinctIterator<RangedIterator<TIter, TItem>, TItem> Distinct()
        => Distinct(null);

    public DistinctIterator<RangedIterator<TIter, TItem>, TItem> Distinct(IEqualityComparer<TItem>? comp)
        => new(this, comp);

    public DistinctIterator<RangedIterator<TIter, TItem>, TItem> DistinctBy<T>(Func<TItem, T> keySelector)
        where T : notnull
        => new(this, Equality.By(keySelector));

    public ConcatIterator<RangedIterator<TIter, TItem>, TOther, TItem> Concat<TOther>(TOther other)
        where TOther : IRefIterator<TOther, TItem>, allows ref struct
        => new(this, other);

    public ConcatIterator<RangedIterator<TIter, TItem>, ItemIterator<TItem>, TItem> Append(TItem append)
        => new(this, append);

    public ConcatIterator<ItemIterator<TItem>, RangedIterator<TIter, TItem>, TItem> Prepend(TItem prepend)
        => new(prepend, this);

    public static RangedIterator<TIter, TItem> Empty() => new(TIter.Empty(), Range.All, 0);


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
    {
        if (_innerRanged)
            return _source.Min(comp);
        if (Iterator.TryGetMin(this, comp, out var min))
            return min;
        ThrowHelper.ThrowInvalidOperationException();
        return default;
    }

    /// <inheritdoc />
    public TItem Max(IComparer<TItem>? comp)
    {
        if (_innerRanged)
            return _source.Max(comp);
        if (Iterator.TryGetMax(this, comp, out var max))
            return max;
        ThrowHelper.ThrowInvalidOperationException();
        return default;
    }

    /// <inheritdoc />
    public TItem? MinOrDefault(IComparer<TItem>? comp)
    {
        if (_innerRanged)
            return _source.MinOrDefault(comp);
        Iterator.TryGetMin(this, comp, out var min);
        return min;
    }

    /// <inheritdoc />
    public TItem? MaxOrDefault(IComparer<TItem>? comp)
    {
        if (_innerRanged)
            return _source.MaxOrDefault(comp);
        Iterator.TryGetMax(this, comp, out var min);
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