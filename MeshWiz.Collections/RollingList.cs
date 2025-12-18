using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Collections;

/// <summary>
/// Alternative to the much more costly LinkedList's Prepend/Append Functionality
/// </summary>
/// <typeparam name="T">Typeof Items</typeparam>
public sealed class RollingList<T> : IVersionedList<T>, IReadOnlyList<T>
{
    private const int DefaultCapacity = 16;
    private T[] _items;
    private int _headIndex;
    private int _postTailIndex;
    public int Version { get; private set; }

    /// <inheritdoc />
    bool ICollection<T>.Remove(T item) => ThrowHelper.ThrowNotSupportedException<bool>();

    public int Count { get; private set; }

    /// <inheritdoc />
    public bool IsReadOnly => false;

    public int Capacity
    {
        get => _items.Length;
        private set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, Count);
            if (value == Capacity) return;
            var result = new T[value];
            CopyTo(result, 0);
            _items = result;
            _headIndex = 0;
            _postTailIndex = Count;
        }
    }

    public RollingList(int capacity)
    {
        _items = new T[capacity];
        Count = 0;
    }

    public RollingList(IEnumerable<T> collection)
    {
        _items = collection.ToArray();
        Count = _items.Length;
        _headIndex = 0;
        _postTailIndex = Count;
    }

    public RollingList(ReadOnlySpan<T> collection)
    {
        _items = collection.ToArray();
        Count = _items.Length;
        _headIndex = 0;
        _postTailIndex = Count;
    }

    public RollingList(T[] source, int startIndex = 0, int count = -1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(startIndex, 0, nameof(startIndex));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(startIndex, source.Length, nameof(startIndex));
        if (count == -1) count = source.Length - startIndex;
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0, nameof(count));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count + startIndex, source.Length, nameof(count));
        var end = startIndex + count;
        _items = source[startIndex..end];
        Count = count;
        _headIndex = 0;
        _postTailIndex = count;
    }

    public RollingList() : this(DefaultCapacity) { }

    public T this[int index]
    {
        get => _items[ValidatedIndex(index)];
        set
        {
            ++Version;
            _items[ValidatedIndex(index)] = value;
        }
    }

    [StackTraceHidden,MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ValidatedIndex(int index)
    {
        if (Count <= (uint)index) IndexThrowHelper.Throw(index, Count);
        index += _headIndex;
        return index < _items.Length ? index : index - _items.Length;
    }


    public T Head => this[0];
    public T Tail => this[Count - 1];

    public void PushFront(T item)
    {
        if (Capacity < Count + 1) Capacity *= 2;
        Version++;
        Count++;
        if (_headIndex <= 0) _headIndex = _items.Length;
        _headIndex--;
        _items[_headIndex] = item;
    }

    public void PushFront(Span<T> span) => PushFront((ReadOnlySpan<T>)span);

    public void PushFront(ReadOnlySpan<T> newItems)
    {
        Version++;
        var capacity = Capacity;
        var newCount = Count + newItems.Length;
        var mustGrow = capacity < newCount;
        if (mustGrow)
        {
            Capacity = newCount.NextPow2();
            _headIndex = _items.Length - newItems.Length;
            var target = _items.AsSpan(_postTailIndex);
            newItems.CopyTo(target);
            Count = newCount;
            return;
        }

        Count = newCount;
        var targetSpan = _items.AsSpan();

        var firstMoveCapacity = _headIndex;
        var firstMoveSize = int.Min(firstMoveCapacity, newItems.Length);
        if (firstMoveCapacity > 0)
        {
            _headIndex -= firstMoveSize;
            var firstMoveTarget = targetSpan[_headIndex..];
            newItems[^firstMoveSize..].CopyTo(firstMoveTarget);
        }

        if (firstMoveSize == newItems.Length) return;

        var secondMoveSize = newItems.Length - firstMoveSize;
        _headIndex = _items.Length - secondMoveSize;
        newItems[..secondMoveSize].CopyTo(targetSpan[_headIndex..]);
    }


    public void PushBack(T item)
    {
        Version++;
        if (Capacity < Count + 1) Capacity *= 2;
        Count++;
        _postTailIndex++;
        if (_postTailIndex > _items.Length) _postTailIndex = 1;
        _items[_postTailIndex - 1] = item;
    }


    public void PushBack(Span<T> span) => PushBack((ReadOnlySpan<T>)span);

    public void PushBack(ReadOnlySpan<T> newItems)
    {
        Version++;
        if (newItems.Length == 0) return;
        var capacity = Capacity;
        var newCount = Count + newItems.Length;
        var mustGrow = capacity < newCount;

        if (mustGrow)
        {
            Capacity = newCount.NextPow2();
            var originalTail = _postTailIndex;
            _postTailIndex += newItems.Length;
            var target = _items.AsSpan()[originalTail.._postTailIndex];
            newItems.CopyTo(target);
            Count = newCount;
            return;
        }

        Count = newCount;

        var targetSpan = _items.AsSpan();

        var firstMoveCapacity = Capacity - _postTailIndex;
        var firstMoveSize = int.Min(firstMoveCapacity, newItems.Length);
        if (firstMoveCapacity > 0)
        {
            var originalTail = _postTailIndex;
            _postTailIndex += firstMoveSize;
            var firstMoveTarget = targetSpan[originalTail..];
            newItems[..firstMoveSize].CopyTo(firstMoveTarget);
        }

        if (firstMoveSize == newItems.Length) return;

        var secondMoveSize = newItems.Length - firstMoveSize;
        _postTailIndex = secondMoveSize;
        newItems[firstMoveSize..].CopyTo(targetSpan[.._postTailIndex]);
    }


    public void Add(T item) => PushBack(item);

    public T PopFront()
    {
        if (Count == 0) ThrowHelper.ThrowInvalidOperationException();
        return PopFrontUnchecked();
    }

    private T PopFrontUnchecked()
    {
        Version++;
        Count--;
        ref var head = ref _items[_headIndex];
        var front = head;
        head = default!;

        _headIndex = _headIndex >= _items.Length - 1 ? 0 : _headIndex + 1;
        return front;
    }

    public T PopBack()
    {
        if (Count == 0) ThrowHelper.ThrowInvalidOperationException();
        return PopBackUnchecked();
    }

    private T PopBackUnchecked()
    {
        Version++;
        Count--;
        _postTailIndex--;
        ref var tail = ref _items[_postTailIndex];
        var back = tail;
        tail = default!;
        if (_postTailIndex < 1) _postTailIndex = _items.Length;
        return back!;
    }

    public bool TryPopFront([NotNullWhen(returnValue: true)] out T? item)
    {
        if (Count == 0)
        {
            item = default!;
            return false;
        }

        item = PopFrontUnchecked()!;
        return true;
    }

    public bool TryPopBack([NotNullWhen(returnValue: true)] out T? item)
    {
        if (Count == 0)
        {
            item = default!;
            return false;
        }

        item = PopBackUnchecked()!;
        return true;
    }

    public T[] ToArray()
    {
        if (Count == 0) return [];

        var result = new T[Count];
        CopyTo(result, 0);
        return result;
    }

    public void Clear()
    {
        Version++;
        if(!TypeOf<T>.Unmanaged)
            Array.Fill(_items, default!);
        _headIndex = 0;
        _postTailIndex = 0;
        Count = 0;
    }

    /// <inheritdoc />
    public bool Contains(T item)
    {
        if (item is null)
        {
            for (var i = 0; i < Count; i++)
                if (this[i] is null)
                    return true;
            return false;
        }

        for (var i = 0; i < Count; i++)
        {
            var current = this[i];
            if (current is not null && item.Equals(current))
                return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void CopyTo(T[] array, int arrayIndex)
    {
        if (Count == 0) return;
        var itemSpan = _items.AsSpan();
        var overflow = _headIndex >= _postTailIndex;
        var firstChunk = overflow
            ? itemSpan[_headIndex..itemSpan.Length]
            : itemSpan[_headIndex.._postTailIndex];
        firstChunk.CopyTo(array.AsSpan(arrayIndex));

        var remainder = Count - firstChunk.Length;
        if (remainder < 1) return;

        var secondChunk = itemSpan[..remainder];
        secondChunk.CopyTo(array.AsSpan(arrayIndex + firstChunk.Length));
    }

    /// <inheritdoc />
    public int IndexOf(T item)
    {
        if (item is null)
        {
            for (var i = 0; i < Count; i++)
                if (this[i] is null)
                    return i;
            return -1;
        }

        for (var i = 0; i < Count; i++)
        {
            var current = this[i];
            if (current is not null && item.Equals(current))
                return i;
        }

        return -1;
    }

    /// <inheritdoc />
    void IList<T>.Insert(int index, T item) => ThrowHelper.ThrowNotSupportedException();

    /// <inheritdoc />
    void IList<T>.RemoveAt(int index) => ThrowHelper.ThrowNotSupportedException();

    public void Trim() => Capacity = Count;


    public Enumerator<RollingList<T>,T> GetEnumerator() => new(this);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
    }

    
}