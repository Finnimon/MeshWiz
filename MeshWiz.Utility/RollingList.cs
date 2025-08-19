using System.Collections;
using System.Runtime.CompilerServices;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Utility;

/// <summary>
/// Alternative to the much more costly LinkedList's Prepend/Append Functionality
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class RollingList<T> : IReadOnlyList<T>
{
    private const int DefaultCapacity = 16;
    private T[] _items;
    private int _headIndex;
    private int _postTailIndex;
    public int Count { get; private set; }

    public int Capacity => _items.Length;

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

    public RollingList(T[] source, int start = 0, int count = -1)
    {
        if (start < 0 || start >= source.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (count == -1) count = source.Length - start;
        if (count < 0 || count + start > source.Length) throw new ArgumentOutOfRangeException(nameof(count));
        var end = start + count;
        _items = source[start..end];
        Count = count;
        _headIndex = 0;
        _postTailIndex = count;
    }

    public RollingList() : this(DefaultCapacity) { }

    public T this[int index]
    {
        get => _items[ValidatedIndex(index)];
        set => _items[ValidatedIndex(index)] = value;
    }

    public ref readonly T this[uint index] => ref _items[ValidatedIndex((int)index)];

    private int ValidatedIndex(int index)
    {
        if (index.InsideInclusiveRange(0, Count - 1))
        {
            index += _headIndex;
            return index < _items.Length ? index : index - _items.Length;
        }

        throw new IndexOutOfRangeException();
    }


    public void PushFront(T item)
    {
        Count++;
        GrowAsNeeded();
        if (_headIndex <= 0) _headIndex = _items.Length;
        _headIndex--;
        _items[_headIndex] = item;
    }

    private void GrowAsNeeded()
    {
        if (Count <= _items.Length) return;

        var arrayLength = _items.Length;
        var newSize = 2 * arrayLength;
        if (_headIndex == 0)
        {
            Array.Resize(ref _items, newSize);
            return;
        }

        var newArray = new T[newSize];
        var firstMoveSize = arrayLength - _headIndex;
        Array.Copy(_items, _headIndex, newArray, 0, firstMoveSize);
        if (_postTailIndex <= _headIndex)
            Array.Copy(_items, 0, newArray, firstMoveSize, _postTailIndex);
        _items = newArray;
        _headIndex = 0;
        _postTailIndex = Count - 1;
    }

    public void PushBack(T item)
    {
        Count++;
        GrowAsNeeded();
        _postTailIndex++;
        if (_postTailIndex > Capacity) _postTailIndex = 1;
        _items[_postTailIndex - 1] = item;
    }

    public void Add(T item) => PushBack(item);

    public T PopFront()
    {
        if (Count == 0) throw new InvalidOperationException();
        return PopFrontUnchecked();
    }

    private T PopFrontUnchecked()
    {
        Count--;
        ref var head=ref _items[_headIndex];
        var front = head ;
        head = default!;

        _headIndex = _headIndex >= _items.Length - 1 ? 0 : _headIndex + 1;
        return front;
    }

    public T PopBack()
    {
        if (Count == 0) throw new InvalidOperationException();
        return PopBackUnchecked();
    }

    private T PopBackUnchecked()
    {
        Count--;
        _postTailIndex--;
        ref var tail = ref _items[_postTailIndex];
        var back = tail;
        tail = default!;
        if (_postTailIndex < 1) _postTailIndex = _items.Length;
        return back!;
    }

    public bool TryPopFront(out T item)
    {
        if (Count == 0)
        {
            item = default!;
            return false;
        }

        item = PopFrontUnchecked();
        return true;
    }

    public bool TryPopBack(out T item)
    {
        if (Count == 0)
        {
            item = default!;
            return false;
        }

        item = PopBackUnchecked();
        return true;
    }

    public T[] ToArrayFast()
    {
        if (Count == 0) return [];

        if (_postTailIndex == 0 || _postTailIndex > _headIndex)
            return _items[_headIndex..(_headIndex + Count)];

        var result = new T[Count];

        var firstMoveSize = int.Min(Count, _items.Length - _headIndex);

        Array.Copy(_items, _headIndex, result, 0, firstMoveSize);
        if (firstMoveSize == Count) return result;
        Array.Copy(_items, 0, result, firstMoveSize, _postTailIndex);
        return result;
    }

    public IEnumerator<T> GetEnumerator()
    {
        if (Count == 0) yield break;
        for (var i = 0; i < Count; i++) yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}