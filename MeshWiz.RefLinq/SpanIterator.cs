using System.Collections;
using System.Runtime.InteropServices;

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

    /// <inheritdoc />
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
}