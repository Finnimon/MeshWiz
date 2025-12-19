using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.RefLinq;

public ref struct SelectManyIterator<TIter, TInner, TIn, TOut>(TIter source, Func<TIn, TInner> flattener)
    : IRefIterator<SelectManyIterator<TIter, TInner, TIn, TOut>, TOut>
    where TIter : IRefIterator<TIter, TIn>, allows ref struct
    where TInner : IEnumerator<TOut>, allows ref struct

{
    private TIter _source = source;
    private TInner? _inner;
    private bool _hasInner;
    private readonly Func<TIn, TInner> _flattener = flattener;


    /// <inheritdoc />
    object? IEnumerator.Current => Current;

    public TOut Current => _inner!.Current;
    public bool MoveNext() => _hasInner && _inner!.MoveNext() || MoveNextRare();

    private bool MoveNextRare()
    {
        if (_hasInner) _inner!.Dispose();
        var success = _source.MoveNext();
        if (!success)
        {
            _hasInner = false;
            return false;
        }

        _inner = _flattener(_source.Current);
        _hasInner = true;
        return _inner.MoveNext();
    }

    /// <inheritdoc />
    public void Reset()
    {
        _source.Reset();
        if (_hasInner)
            _inner!.Dispose();
        _hasInner = false;
    }

    /// <inheritdoc />
    public void Dispose() => _source.Dispose();

    /// <inheritdoc />
    public TOut First() => TryGetFirst(out var first) ? first! : ThrowHelper.ThrowInvalidOperationException<TOut>();

    /// <inheritdoc />
    public TOut? FirstOrDefault()
    {
        TryGetFirst(out var first);
        return first;
    }

    /// <inheritdoc />
    public bool TryGetFirst(out TOut? item) => Iterator.TryGetFirst(this, out item);

    /// <inheritdoc />
    public TOut Last() => TryGetLast(out var last) ? last! : ThrowHelper.ThrowInvalidOperationException<TOut>();

    /// <inheritdoc />
    public TOut? LastOrDefault()
    {
        TryGetLast(out var last);
        return last;
    }

    /// <inheritdoc />
    public bool TryGetLast(out TOut? item)
    {
        item = default;
        if (!_source.TryGetLast(out var lastSource))
            return false;
        var lastInner = _flattener(lastSource!);
        return lastInner.TryConvertToSpanIter<TInner, TOut>(out var spanIterator) && spanIterator.TryGetLast(out item)
               || Iterator.TryGetLast(lastInner, out item);
    }

    /// <inheritdoc />
    public int Count()
    {
        this.Reset();
        return !IsSpanner ? IterativeCount() : SpannerCount();
    }

    private int IterativeCount()
    {
        var count = 0;
        while (_source.MoveNext())
        {
            var inner = _flattener(_source.Current);
            while (inner.MoveNext()) count++;
            inner.Dispose();
        }

        return count;
    }

    private int SpannerCount()
    {
        var count = 0;
        var spanner = AsSpanner();
        while (spanner._source.MoveNext())
        {
            var inner = spanner._flattener(spanner._source.Current);
            count += inner.Count();
        }

        return count;
    }


    private static bool IsSpanner => typeof(TInner) == typeof(SpanIterator<TOut>);

    /// <inheritdoc />
    public bool TryGetNonEnumeratedCount(out int count) => _source.TryGetNonEnumeratedCount(out count) && count == 0;

    /// <inheritdoc />
    public void CopyTo(Span<TOut> destination)
    {
        Reset();
        if (IsSpanner)
            Iterator.CopyTo(this, destination);
        else
            SpannerCopyTo(destination);
        Reset();
    }

    private void SpannerCopyTo(Span<TOut> destination)
    {
        var pos = 0;
        var spanner = AsSpanner();
        spanner.Reset();
        while (spanner._source.MoveNext())
        {
            var curInner = spanner._flattener(spanner._source.Current);
            var curSpan = curInner.OriginalSource;
            curSpan.CopyTo(destination[pos..]);
            pos += curSpan.Length;
        }
    }

    /// <inheritdoc />
    public SelectManyIterator<TIter, TInner, TIn, TOut> GetEnumerator()
    {
        var copy = this;
        copy.Reset();
        return copy;
    }

    /// <inheritdoc />
    public FilterIterator<SelectManyIterator<TIter, TInner, TIn, TOut>, TOut> Where(Func<TOut, bool> predicate)
        => new(this, predicate);

    /// <inheritdoc />
    public SelectIterator<SelectManyIterator<TIter, TInner, TIn, TOut>, TOut, TOut1> Select<TOut1>(
        Func<TOut, TOut1> selector)
        => new(this, selector);


    public SelectManyIterator<SelectManyIterator<TIter, TInner, TIn, TOut>, TInner2, TOut, TMany> SelectMany<TInner2,
        TMany>(
        Func<TOut, TInner2> flattener2) where TInner2 : IEnumerator<TMany>, allows ref struct =>
        new(this, flattener2);

    public SelectManyIterator<SelectManyIterator<TIter, TInner, TIn, TOut>, SpanIterator<TMany>, TOut, TMany>
        SelectMany<TMany>(
            Func<TOut, TMany[]> flattener2) => new(this, inner => flattener2(inner));

    public SelectManyIterator<SelectManyIterator<TIter, TInner, TIn, TOut>, SpanIterator<TMany>, TOut, TMany>
        SelectMany<TMany>(
            Func<TOut, List<TMany>> flattener2) => new(this, inner => flattener2(inner));

    public SelectManyIterator<SelectManyIterator<TIter, TInner, TIn, TOut>, IEnumerator<TMany>, TOut, TMany>
        SelectMany<TMany>(
            Func<TOut, IEnumerable<TMany>> flattener2) => new(this, inner => flattener2(inner).GetEnumerator());


    /// <inheritdoc />
    public RangeIterator<SelectManyIterator<TIter, TInner, TIn, TOut>, TOut> Take(Range r)
        => new(this, r);

    /// <inheritdoc />
    public RangeIterator<SelectManyIterator<TIter, TInner, TIn, TOut>, TOut> Take(int num)
        => new(this, ..num);

    /// <inheritdoc />
    public RangeIterator<SelectManyIterator<TIter, TInner, TIn, TOut>, TOut> Skip(int num)
        => new(this, num..);

    /// <inheritdoc />
    public TOut[] ToArray() => IsSpanner
        ? SpannerToArray()
        : Iterator.ToArray<SelectManyIterator<TIter, TInner, TIn, TOut>, TOut>(this);

    private TOut[] SpannerToArray()
    {
        var spanner = AsSpanner();
        spanner.Reset();
        
        SegmentedArrayBuilder<TOut> builder = new();
        while (spanner._source.MoveNext())
        {
            var curSpan = spanner._flattener(spanner._source.Current).OriginalSource;
            builder.AddRange(curSpan);
        }
        return builder.ToArray();
    }

    private SelectManyIterator<TIter, SpanIterator<TOut>, TIn, TOut> AsSpanner()
    {
        return Unsafe.As<SelectManyIterator<TIter, TInner, TIn, TOut>,
            SelectManyIterator<TIter, SpanIterator<TOut>, TIn, TOut>>(ref this);
    }

    private List<TOut> SpannerToList()
    {
        var spanner = AsSpanner();
        spanner.Reset();
        
        SegmentedArrayBuilder<TOut> builder = new(default);
        while (spanner._source.MoveNext())
        {
            var curSpan = spanner._flattener(spanner._source.Current).OriginalSource;
            builder.AddRange(curSpan);
        }
        return builder.ToList();
    }

    /// <inheritdoc />
    public List<TOut> ToList()
        => IsSpanner ? SpannerToList() : Iterator.ToList<SelectManyIterator<TIter, TInner, TIn, TOut>, TOut>(this);

    /// <inheritdoc />
    public HashSet<TOut> ToHashSet() => [..ToArray()];

    /// <inheritdoc />
    public HashSet<TOut> ToHashSet(IEqualityComparer<TOut> comp)
        => new(ToArray(), comp);

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
        using var copy = this.GetEnumerator();
        return copy.MoveNext();
    }

    /// <inheritdoc />
    public bool Any(Func<TOut, bool> predicate)
        => Where(predicate).Any();

    /// <inheritdoc />
    public int EstimateCount() => _source.EstimateCount() * 16;

    /// <inheritdoc />
    public OfTypeIterator<SelectManyIterator<TIter, TInner, TIn, TOut>, TOut, TOther> OfType<TOther>() => new(this);

    /// <inheritdoc />
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

    /// <inheritdoc />
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
        ToDictionary(keyGen, valGen, null);

    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
        Func<TOut, TKey> keyGen,
        Func<TOut, TValue> valGen,
        IEqualityComparer<TKey>? comp)
        where TKey : notnull =>
        Iterator.ToDictionary(this, comp, keyGen, valGen);

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