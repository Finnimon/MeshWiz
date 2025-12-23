using System.Collections;
using MeshWiz.Utility;

namespace MeshWiz.RefLinq;

public record AdapterIterator<T>(IEnumerable<T> Source) : IRefIterator<AdapterIterator<T>,T>
{
    private IEnumerator<T>? _enumerator;

    /// <inheritdoc />
    public bool MoveNext()
    {
        _enumerator ??= Source.GetEnumerator();
        return _enumerator.MoveNext();
    }

    /// <inheritdoc />
    public void Reset()
    {
        _enumerator?.Dispose();
        _enumerator = null;
    }

    /// <inheritdoc />
    public T Current => _enumerator!.Current;

    /// <inheritdoc />
    object? IEnumerator.Current => Current;

    /// <inheritdoc />
    public void Dispose()
    {
        Reset();
        if (Source is not IDisposable d) return;
        d.Dispose();
    }

    /// <inheritdoc />
    public T First() => Source.First();

    /// <inheritdoc />
    public T? FirstOrDefault()
        => Source.FirstOrDefault();

    /// <inheritdoc />
    public bool TryGetFirst(out T? item)
    {
        using var enumerator = Source.GetEnumerator();
        var result = enumerator.MoveNext();
        item = result ? enumerator.Current : default;
        return result;
    }

    /// <inheritdoc />
    public T Last()
        => Source.Last();

    /// <inheritdoc />
    public T? LastOrDefault()
        => Source.LastOrDefault();

    /// <inheritdoc />
    public bool TryGetLast(out T? item)
        => Func.Try(Enumerable.Last, Source).TryGetValue<ExceptionResult<T>, Exception, T>(out item);

    /// <inheritdoc />
    public int Count()
        => Source.Count();

    /// <inheritdoc />
    public bool TryGetNonEnumeratedCount(out int count) => Source.TryGetNonEnumeratedCount(out count);

    /// <inheritdoc />
    public void CopyTo(Span<T> destination)
    {
        using var enumerator = Source.GetEnumerator();
        var i = -1;
        while (enumerator.MoveNext())
            destination[++i] = enumerator.Current;
    }

    /// <inheritdoc />
    public AdapterIterator<T> GetEnumerator()
    {
        var copy = this;
        copy.Reset();
        return copy;
    }

    /// <inheritdoc />
    public WhereIterator<AdapterIterator<T>, T> Where(Func<T, bool> predicate)
        => new(this, predicate);

    /// <inheritdoc />
    public SelectIterator<AdapterIterator<T>, T, TOut> Select<TOut>(Func<T, TOut> selector)
        => new(this, selector);

    /// <inheritdoc />
    public RangeIterator<AdapterIterator<T>, T> Take(Range r)
        => new(this, r);

    /// <inheritdoc />
    public RangeIterator<AdapterIterator<T>, T> Take(int num)
        => Take(..num);

    /// <inheritdoc />
    public RangeIterator<AdapterIterator<T>, T> Skip(int num)
        => Take(num..);

    /// <inheritdoc />
    public T[] ToArray()
        => Source.ToArray();

    /// <inheritdoc />
    public List<T> ToList()
        => Source.ToList();

    /// <inheritdoc />
    public HashSet<T> ToHashSet()
        => ToHashSet(null);

    /// <inheritdoc />
    public HashSet<T> ToHashSet(IEqualityComparer<T>? comp)
        => Source.ToHashSet(comp);

    /// <inheritdoc />
    public int EstimateCount() => TryGetNonEnumeratedCount(out var count) ? count : 64;

    /// <inheritdoc />
    public OfTypeIterator<AdapterIterator<T>, T, TOther> OfType<TOther>()
        => new(this);

    /// <inheritdoc />
    public T Aggregate(Func<T, T, T> aggregator)
        => Source.Aggregate(aggregator);

    /// <inheritdoc />
    public TOther Aggregate<TOther>(Func<TOther, T, TOther> aggregator, TOther seed)
        => Source.Aggregate(seed,aggregator);

    /// <inheritdoc />
    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(Func<T, TKey> keyGen, Func<T, TValue> valGen,
        IEqualityComparer<TKey>? comp) where TKey : notnull
        => Source.ToDictionary(keyGen, valGen, comp);

    /// <inheritdoc />
    public SelectManyIterator<AdapterIterator<T>, TInner, T, TOut> SelectMany<TInner, TOut>(Func<T, TInner> flattener) where TInner : IRefIterator<TInner,TOut>, allows ref struct => new(this, flattener);

    /// <inheritdoc />
    public SelectManyIterator<AdapterIterator<T>, SpanIterator<TOut>, T, TOut> SelectMany<TOut>(Func<T, TOut[]> flattener)
        => new(this, x=>flattener(x));

    /// <inheritdoc />
    public SelectManyIterator<AdapterIterator<T>, SpanIterator<TOut>, T, TOut> SelectMany<TOut>(
        Func<T, List<TOut>> flattener)
        => new(this, x=>flattener(x));

    /// <inheritdoc />
    public SelectManyIterator<AdapterIterator<T>, AdapterIterator<TOut>, T, TOut> SelectMany<TOut>(Func<T, IEnumerable<TOut>> flattener) => new(this, x=>new(flattener(x)));

    /// <inheritdoc />
    public bool TryTakeRange(Range r, out AdapterIterator<T> result)
    {
        var range = Source.Take(r);
        result = new AdapterIterator<T>(range);
        return true;
    }
    
    
    public DistinctIterator<AdapterIterator<T>, T> Distinct()
        => Distinct(null);
    public DistinctIterator<AdapterIterator<T>, T> Distinct(IEqualityComparer<T>? comp)
        => new(this, comp);
    public DistinctIterator<AdapterIterator<T>,T> DistinctBy<TKey>(Func<T, TKey> keySelector) where TKey : notnull 
        =>new(this,Equality.By(keySelector));

    public ConcatIterator<AdapterIterator<T>, TOther, T> Concat<TOther>(TOther other) 
        where TOther : IRefIterator<TOther, T>,allows ref struct
        => new(this, other);
    public ConcatIterator<AdapterIterator<T>, ItemIterator<T>, T> Append(T append) 
        => new(this, append);
    public ConcatIterator<ItemIterator<T>,AdapterIterator<T>, T> Prepend(T prepend) 
        => new(prepend,this);
}