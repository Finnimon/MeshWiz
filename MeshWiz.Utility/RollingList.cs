using System.Collections;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Utility;

/// <summary>
/// Alternative to the much more costly LinkedList's Prepend/Append Functionality
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class RollingList<T> : IReadOnlyList<T>
    where T : unmanaged
{
    private const int DefaultCapacity = 16;
    private const int DefaultGrowthCap = 9192;
    private T[] _items;
    private readonly int _growthCap;
    private int _headIndex;
    private int _postTailIndex;
    public int Count { get; private set; }

    public int Capacity => _items.Length;

    public RollingList(int capacity, int growthCap)
    {
        _items = new T[capacity];
        Count = 0;
        _growthCap = int.Max(256, growthCap);
    }

    public RollingList(T[] source, int start = 0, int count = -1, int growthCap = DefaultGrowthCap)
    {
        if (start < 0 || start >= source.Length) throw new ArgumentOutOfRangeException(nameof(start));
        if (count == -1) count = source.Length - start;
        if (count < 0 || count + start > source.Length) throw new ArgumentOutOfRangeException(nameof(count));
        _items = new T[count];
        Array.Copy(source, start, _items, 0, count);
        Count = count;
        _headIndex = 0;
        _postTailIndex = count;
        _growthCap = growthCap;
    }

    public RollingList() : this(DefaultCapacity, DefaultGrowthCap) { }

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
        var newSize = arrayLength + int.Min(_growthCap, arrayLength);
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
        _headIndex = firstMoveSize;
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
        var front = _items[_headIndex];
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
        var back = _items[_postTailIndex - 1];
        _postTailIndex = _postTailIndex <= 1 ? _items.Length : _postTailIndex - 1;
        return back;
    }

    public bool TryPopFront(out T item)
    {
        if (Count == 0)
        {
            item = default;
            return false;
        }

        item = PopFrontUnchecked();
        return true;
    }

    public bool TryPopBack(out T item)
    {
        if (Count == 0)
        {
            item = default;
            return false;
        }

        item = PopBackUnchecked();
        return true;
    }


    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < Count; i++) yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}