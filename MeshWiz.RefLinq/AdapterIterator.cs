using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Buffers;
using MeshWiz.Utility;

namespace MeshWiz.RefLinq;

public readonly partial struct AdapterIterator<T> : IRefIterator<AdapterIterator<T>,T>, IEnumerable<T>
{
    internal readonly Imp.IImp _imp;
    public AdapterIterator(IEnumerable<T> source) => _imp = Imp.Create(source);
    internal AdapterIterator(Imp.IImp imp) => _imp = imp;

    /// <inheritdoc />
    public bool MoveNext() => _imp.MoveNext();

    /// <inheritdoc />
    public void Reset() => _imp.Reset();

    /// <inheritdoc />
    public T Current => _imp.Current;

    /// <inheritdoc />
    object? IEnumerator.Current => _imp.Current;

    /// <inheritdoc />
    public void Dispose() => _imp.Dispose();

    /// <inheritdoc />
    public T First() => Iterator.First<AdapterIterator<T>, T>(this);

    /// <inheritdoc />
    public T? FirstOrDefault()
    {
        TryGetFirst(out var first);
        return first;
    }

    /// <inheritdoc />
    public bool TryGetFirst(out T? item)
        => _imp.TryGetFirst(out item);

    /// <inheritdoc />
    public T Last()
        => TryGetLast(out var last)? last!: ThrowHelper.ThrowInvalidOperationException<T>();

    /// <inheritdoc />
    public T? LastOrDefault()
    {
        TryGetLast(out var last);
        return last;
    }

    /// <inheritdoc />
    public bool TryGetLast(out T? item) => _imp.TryGetLast(out item);

    /// <inheritdoc />
    public int Count()
        => _imp.Count();

    /// <inheritdoc />
    public bool TryGetNonEnumeratedCount(out int count) => _imp.TryGetNonEnumeratedCount(out count);

    /// <inheritdoc />
    public void CopyTo(Span<T> destination) => _imp.CopyTo(destination);

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

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
    {
        if (_imp.TryGetNonEnumeratedCount(out var count))
        {
            var arr = GC.AllocateUninitializedArray<T>(count);
            _imp.CopyTo(arr);
            return arr;
        }

        return ArrayBuilder.Helper<T>.ToArray(_imp.Underlying);
    }

    public bool TryGetSpan(out ReadOnlySpan<T> span) => _imp.TryGetSpan(out span);

    /// <inheritdoc />
    public List<T> ToList()
    {
        if (_imp.TryGetNonEnumeratedCount(out var count))
        {
            List<T> list = new(count);
            CollectionsMarshal.SetCount(list,count);
            _imp.CopyTo(CollectionsMarshal.AsSpan(list));
            return list;
        }

        return ArrayBuilder.Helper<T>.ToList(_imp.Underlying);
    }
    /// <inheritdoc />
    public HashSet<T> ToHashSet()
        => ToHashSet(null);

    /// <inheritdoc />
    public HashSet<T> ToHashSet(IEqualityComparer<T>? comp)
        => Iterator.ToHashSet(this, comp);

    /// <inheritdoc />
    public int EstimateCount() => TryGetNonEnumeratedCount(out var count) ? count : 8;

    /// <inheritdoc />
    public OfTypeIterator<AdapterIterator<T>, T, TOther> OfType<TOther>()
        => new(this);

    /// <inheritdoc />
    public T Aggregate(Func<T, T, T> aggregator)
        => Iterator.Aggregate(this, aggregator);

    /// <inheritdoc />
    public TOther Aggregate<TOther>(Func<TOther, T, TOther> aggregator, TOther seed)
        => Iterator.Aggregate(this, aggregator,seed);

    /// <inheritdoc />
    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(Func<T, TKey> keyGen, Func<T, TValue> valGen,
        IEqualityComparer<TKey>? comp) where TKey : notnull
        => Iterator.ToDictionary(this,comp,keyGen, valGen);

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
        if (!_imp.TryTakeRange(r, out var newImp))
        {
            Unsafe.SkipInit(out result);
            return false;
        }
        result = new AdapterIterator<T>(newImp!);
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
    public static AdapterIterator<T> Empty()=>new([]);
    
    
    /// <inheritdoc />
    public T Min()
        => Min(null);

    /// <inheritdoc />
    public T Max()
        => Max(null);

    /// <inheritdoc />
    public T? MinOrDefault()
        => MinOrDefault(null);

    /// <inheritdoc />
    public T? MaxOrDefault()
        => MaxOrDefault(null);

    /// <inheritdoc />
    public T Min(IComparer<T>? comp)
        =>Iterator.TryGetMin(this,comp,out var min)?min:ThrowHelper.ThrowInvalidOperationException<T>();

    /// <inheritdoc />
    public T Max(IComparer<T>? comp)
        =>Iterator.TryGetMax(this,comp,out var min)?min:ThrowHelper.ThrowInvalidOperationException<T>();

    /// <inheritdoc />
    public T? MinOrDefault(IComparer<T>? comp)
    {
        Iterator.TryGetMin(this,comp,out var min);
        return min;
    }

    /// <inheritdoc />
    public T? MaxOrDefault(IComparer<T>? comp)
    {
        Iterator.TryGetMax(this,comp,out var min);
        return min;
    }


    public T MinBy<TKey>(Func<T, TKey> bySel) where TKey : IComparable<TKey> => Min(Equality.CompareBy(bySel));

    /// <inheritdoc />
    public T MaxBy<TKey>(Func<T, TKey> bySel) where TKey : IComparable<TKey> => Max(Equality.CompareBy(bySel));

    /// <inheritdoc />
    public T? MinOrDefaultBy<TKey>(Func<T, TKey> bySel) where TKey : IComparable<TKey>
        => MinOrDefault(Equality.CompareBy(bySel));

    /// <inheritdoc />
    public T? MaxOrDefaultBy<T1>(Func<T, T1> bySel) where T1 : IComparable<T1> => MaxOrDefault(Equality.CompareBy(bySel));

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}