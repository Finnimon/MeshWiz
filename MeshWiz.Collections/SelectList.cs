using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.Collections;

public readonly struct SelectList<TIn, TOut> : IVersionedList<TOut>, IReadOnlyList<TOut>
{
    public int Count => _source.Count;
    private readonly IReadOnlyList<TIn> _source;
    private readonly Func<TIn, TOut> _selector;

    public SelectList(IReadOnlyList<TIn> source, Func<TIn, TOut> selector)
    {
        _source = source;
        _selector = selector;
    }

    /// <inheritdoc />
    bool ICollection<TOut>.IsReadOnly => true;

    /// <inheritdoc />
    void ICollection<TOut>.Add(TOut item) => ThrowHelper.ThrowNotSupportedException();

    /// <inheritdoc />
    void ICollection<TOut>.Clear() => ThrowHelper.ThrowNotSupportedException();

    /// <inheritdoc />
    bool ICollection<TOut>.Contains(TOut item)
    {
        var comp = EqualityComparer<TOut>.Default;
        // ReSharper disable once LoopCanBeConvertedToQuery
        for (var i = 0; i < Count; i++)
            if (comp.Equals(item, this[i]))
                return true;
        return false;
    }

    /// <inheritdoc />
    public void CopyTo(TOut[] array, int arrayIndex)
    {
        if (arrayIndex < 0 || array.Length - arrayIndex < Count)
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(arrayIndex));
        for (var i = 0; i < Count; i++)
            array[arrayIndex + i] = this[i];
    }

    /// <inheritdoc />
    bool ICollection<TOut>.Remove(TOut item) => ThrowHelper.ThrowNotSupportedException<bool>();


    /// <inheritdoc />
    int IList<TOut>.IndexOf(TOut item)
    {
        var comp = EqualityComparer<TOut>.Default;
        for (var i = 0; i < Count; i++)
            if (comp.Equals(this[i], item))
                return i;
        return -1;
    }

    /// <inheritdoc />
    void IList<TOut>.Insert(int index, TOut item) => ThrowHelper.ThrowNotSupportedException();

    /// <inheritdoc />
    void IList<TOut>.RemoveAt(int index) => ThrowHelper.ThrowNotSupportedException();

    /// <inheritdoc />
    TOut IList<TOut>.this[int index]
    {
        get => this[index];
        // ReSharper disable once ValueParameterNotUsed
        set => ThrowHelper.ThrowNotSupportedException();
    }

    public TOut this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _selector(_source[index]);
    }

    /// <inheritdoc />
    int IVersionedList<TOut>.Version => 0;

    public Enumerator<SelectList<TIn, TOut>, TOut> GetEnumerator() => new(this);
}