using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Buffers;

namespace MeshWiz.RefLinq;

[SuppressMessage("ReSharper", "NotDisposedResource")]
public ref struct BufferedArrayBuilder<T>
{
    private Arrays _laterSegments;
    private Freelist.Buffer<T> _firstSegment;
    private Span<T> _currentSegment;
    private int _size;
    private int _poolBufCount;
    private int _curSegmentPosition = -1;
    public readonly bool OnFirstSegment
    {
        [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _poolBufCount == 0;
    }

    public readonly int Count => _size;
    public const int MinInitialSize = 128;

    public BufferedArrayBuilder()
    {
        _firstSegment = Freelist.Shared.Rent<T>(MinInitialSize);
        _currentSegment = _firstSegment.Span;
    }

    public BufferedArrayBuilder(int capacity)
    {
        _firstSegment = Freelist.Shared.Rent<T>(int.Max(capacity,MinInitialSize));
        _currentSegment = _firstSegment.Span;
    }
    private BufferedArrayBuilder(int initial, bool _)
    {
        _firstSegment = Freelist.Shared.Rent<T>(initial);
        _currentSegment = _firstSegment.Span;
    }

    public void AddRange(IEnumerable<T> collection)
    {
        if (collection.TryGetSpan(out var span)) AddRangeInlined(span);
        else if (collection.TryGetNonEnumeratedCount(out var count)) AddRange(collection, count);
        else AddEnumeratingInlined(collection);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AddRange(ReadOnlySpan<T> span) => AddRangeInlined(span);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRangeInlined(ReadOnlySpan<T> span)
    {
        if (span.Length == 0) return;
        var space = SpaceInCurrentSeg();
        var gotSpace = space >= span.Length;
        if (!gotSpace && _curSegmentPosition == -1) gotSpace = TryExpandByAtLeast(span.Length - SpaceInCurrentSeg());
        if (!gotSpace)
        {
            AddEnumerating(span);
            return;
        }

        span.CopyTo(_currentSegment.Slice(_curSegmentPosition + 1));
        _curSegmentPosition += span.Length;
        _size += span.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AddEnumerating(ReadOnlySpan<T> collection)
    {
        foreach (var elem in collection) AddInlined(elem);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly int SpaceInCurrentSeg()
    {
        var countInCurSeg = _curSegmentPosition + 1;
        return _currentSegment.Length - countInCurSeg;
    }

    public void AddRange(IReadOnlyCollection<T> c) => AddRange(c, c.Count);
    public void AddRange(ICollection<T> c) => AddRange(c, c.Count);

    private void AddRange(IEnumerable<T> collection, int count)
    {
        if (count == 0) return;
        var space = SpaceInCurrentSeg();
        var gotSpace = space >= count;
        if (!gotSpace && _curSegmentPosition == -1) gotSpace = TryExpandByAtLeast(count - SpaceInCurrentSeg());
        if (!gotSpace)
        {
            AddEnumerating(collection);
            return;
        }

        foreach (var elem in collection) AddAssumingSpace(elem);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AddEnumerating(IEnumerable<T> collection) => AddEnumeratingInlined(collection);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddEnumeratingInlined(IEnumerable<T> collection)
    {
        using var iter = collection.GetEnumerator();
        AddEnumeratorInlined(iter);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AddEnumerator<TIter>(TIter iter) where TIter : IEnumerator<T>, allows ref struct =>
        AddEnumeratorInlined(iter);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddEnumeratorInlined<TIter>(TIter iterator)
        where TIter : IEnumerator<T>, allows ref struct
    {
        while (iterator.MoveNext())
        {
            var current = iterator.Current;
            AddInlined(current);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddInlined(T current)
    {
        ++_size;
        ++_curSegmentPosition;
        if (_currentSegment.Length != _curSegmentPosition)
        {
            _currentSegment[_curSegmentPosition] = current;
            return;
        }

        ExpandAdd(current);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddAssumingSpace(T current)
    {
        ++_size;
        _currentSegment[++_curSegmentPosition] = current;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public readonly void ToSpan(Span<T> target) => ToSpanInlined(target);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void ToSpanInlined(Span<T> target)
    {
        if (_size != target.Length) ThrowHelper.ThrowArgumentException(nameof(target));
        if (_size == 0) return;
        if (OnFirstSegment)
        {
            _firstSegment.Span.Slice(0, _size).CopyTo(target);
            return;
        }

        _firstSegment.Span.CopyTo(target);
        var offset = _firstSegment.Span.Length;
        var countFullPoolBuf = _poolBufCount - 1;
        for (var i = 0; i < countFullPoolBuf; i++)
        {
            var fullPoolBuf = _laterSegments[i];
            fullPoolBuf.Span.CopyTo(target.Slice(offset));
            offset += fullPoolBuf.Span.Length;
        }

        _currentSegment.Slice(0, _curSegmentPosition + 1).CopyTo(target.Slice(offset));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ExpandAdd(T current)
    {
        if (OnFirstSegment && 0!=Freelist.GrowGreedy(in _firstSegment))
        {
            _currentSegment = _firstSegment.Span;
            _currentSegment[_curSegmentPosition] = current;
            return;
        }

        var nextSize = int.Max(1,_currentSegment.Length * 2);
        if (nextSize < 0) ThrowHelper.ThrowInsufficientMemoryException();
        nextSize = int.Min(nextSize, Array.MaxLength);
        var poolBuf = Pool.Rent<T>(nextSize);
        _laterSegments[_poolBufCount++] = poolBuf;
        _curSegmentPosition = 0;
        _currentSegment = poolBuf.Span;
        _currentSegment[0] = current;
    }

    private bool TryExpandByAtLeast(int target)
    {
        if (OnFirstSegment)
        {
            var growth = Freelist.GrowGreedy(in _firstSegment);
            _currentSegment = _firstSegment.Span;
            if (growth != 0) return growth >= target;
        }

        var nextSize = target * 2;
        if (nextSize < 0) ThrowHelper.ThrowInsufficientMemoryException();
        nextSize = int.Min(nextSize, Array.MaxLength);
        var poolBuf = Pool.Rent<T>(nextSize);
        _laterSegments[_poolBufCount++] = poolBuf;
        _curSegmentPosition = -1;
        _currentSegment = poolBuf.Span;
        return nextSize >= target;
    }

    public void Dispose()
    {
        if (_poolBufCount == 0)
        {
            _firstSegment.Dispose(_size);
            return;
        }
        _firstSegment.Dispose();
        for (var i = 0; i < _poolBufCount; i++)
            _laterSegments[i].Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private ref struct Arrays
    {
        // @formatter:off
 #pragma warning disable CS0169 // Field is never used
 #pragma warning disable CS0649 // Field is never used
 // ReSharper disable once UnassignedField.Local
        private Pool.Buffer<T> _0,_1,_2,_3,_4,_5,_6,_7,_8,_9,_10,_11,_12,_13,_14,_15,_16,_17,_18,_19,_20,_21;
 #pragma warning restore CS0649 // Field is never used
 #pragma warning restore CS0169 // Field is never used
        // @formatter:on
        public Pool.Buffer<T> this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Unsafe.Add(ref Unsafe.AsRef(ref _0), index);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set=>Unsafe.Add(ref Unsafe.AsRef(ref _0), index) = value;
        }
    }

    public readonly T[] ToArray()
    {
        if (_size == 0) return [];
        var arr = GC.AllocateUninitializedArray<T>(_size);
        ToSpanInlined(arr);
        return arr;
    }

    public readonly List<T> ToList()
    {
        var count = _size;
        if (count == 0) return [];
        var list = new List<T>(count);
        CollectionsMarshal.SetCount(list, count);
        ToSpanInlined(CollectionsMarshal.AsSpan(list));
        return list;
    }
}