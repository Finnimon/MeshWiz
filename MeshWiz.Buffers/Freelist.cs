using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using JetBrains.Annotations;

namespace MeshWiz.Buffers;

public sealed partial class Freelist
{
    private nuint[] _activeBuffer;


    private readonly WeakSortedList _occupiedChunks;
    public bool IsDense => _occupiedChunks.Count == 1;
    public bool NoneRented => _occupiedChunks.Count == 0;
    public long Capacity => nuint.Size * (long)_activeBuffer.Length;

    /// <summary>
    /// Clearing is not necessary for managed types, as the background storage is always a word array
    /// </summary>
    public readonly bool ClearUponReturn;

    private int _rentedBufCount;
    private int _rentedWordCount;
    private readonly int _initialWordCount;

    public Freelist(long initialByteSize, bool clearUponReturn)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialByteSize, 0, nameof(initialByteSize));
        _initialWordCount = int.Max(1, Utilities.GetWordCount<byte>(initialByteSize));
        _activeBuffer = [];
        _occupiedChunks = new WeakSortedList(4);
        ClearUponReturn = clearUponReturn;
    }

    [MustUseReturnValue,MustDisposeResource]
    public Buffer<T> Rent<T>(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length, nameof(length));
        if (length == 0) return EmptyBuffer<T>();
        var wordCount = Utilities.GetWordCount<T>(length);
        return Buffer<T>.FromWordBuf(RentInternal(wordCount), length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Buffer<nuint> RentInternal(int length)
    {
        if (length < 0 || length > Array.MaxLength) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(length));
        if (length > (_activeBuffer.Length - _rentedWordCount)) return RentOnNewBuffer(length);
        return _occupiedChunks.Count switch
        {
            0 => FastRentFromStart(length),
            1 => FastDenseRent(length),
            _ => FindGapRent(length)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Buffer<T> EmptyBuffer<T>() => new(true, 0, [], null, [], 0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Buffer<nuint> FindGapRent(int len)
    {
        var chunkCount = _occupiedChunks.Count;
        var lastIndex = chunkCount-1;
        var totalEnd = _occupiedChunks.GetValueAtIndex(lastIndex);
        var totalGapsSize = totalEnd - _rentedBufCount;
        var gapCount = lastIndex;
        var maxGapSize = totalGapsSize - gapCount + 1;
        var maybeGapPossible = maxGapSize >= len;
        if (!maybeGapPossible)
        {
            var rem = _activeBuffer.Length-totalEnd;
            if (rem >= len) return RentAfter(lastIndex, len, totalEnd);
            return RentOnNewBuffer(len);
        }
        
        KeyValuePair<int, int> previous = new(0, 0);
        var pos = 0;
        for (var i = 0; i < chunkCount; i++)
        {
            var chunk = _occupiedChunks.GetEntryAtIndex(i);
            var gapSize = chunk.Key - previous.Value;
            if (gapSize > len || i == 0 && gapSize == len)
                return RentBefore(i, len,chunk);
            if (gapSize == len)
                return RentBetween(i - 1, i, len,previous,chunk);
            if (++pos == chunkCount && len <= _activeBuffer.Length - chunk.Value)
                return RentAfter(i, len,chunk.Value);
            previous = chunk;
        }

        return RentOnNewBuffer(len);
    }

    

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Buffer<nuint> RentOnNewBuffer(int length)
    {
        _occupiedChunks.Clear();
        var nextSize = NextAllocSize(length);
        _activeBuffer = GC.AllocateUninitializedArray<nuint>(nextSize);
        _rentedBufCount = 0;
        return FastRentFromStart(length);
    }

    private int NextAllocSize(int length)
    {
        var nextSize = int.Max((length + _activeBuffer.Length).NextPow2(), _activeBuffer.Length * 2);
        nextSize = int.Max(_initialWordCount, nextSize);
        nextSize = int.Min(Array.MaxLength, nextSize);
        return nextSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Buffer<UIntPtr> RentBetween(int chunkBefore, int chunkAfter, int len, KeyValuePair<int, int> previous, KeyValuePair<int, int> chunk)

    {
        var bufStart = previous.Value;
        var chunkEnd = chunk.Key;
        _occupiedChunks.RemoveAt(chunkAfter);
        _occupiedChunks.SetValueAtIndex(chunkBefore, chunkEnd);
        return CreateWordBuf(bufStart, len);
    }

    // private Buffer<nuint> RentBefore(int key, int length)
    // {
    //     var end = _taken[key];
    //     _taken.Remove(key);
    //     var bufStart = key - length;
    //     _taken.Add(bufStart, end);
    //     return CreateWordBuf(bufStart, length);
    // }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Buffer<UIntPtr> RentBefore(int index, int length, KeyValuePair<int, int> chunk)
    {
        var start = chunk.Key;
        var bufStart = start - length;
        _occupiedChunks.SetKeyAtIndex(index, bufStart);
        return CreateWordBuf(bufStart, length);
    }

    // private Buffer<nuint> RentAfter(int key, int length)
    // {
    //     var oldEnd = _taken[key];
    //     var newEnd = oldEnd + length;
    //     _taken[key] = newEnd;
    //     return CreateWordBuf(oldEnd, length);
    // }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Buffer<UIntPtr> RentAfter(int index, int length, int oldEnd)
    {
        var newEnd = oldEnd + length;
        _occupiedChunks.SetValueAtIndex(index, newEnd);
        return CreateWordBuf(oldEnd, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Buffer<nuint> FastDenseRent(int length)
    {
        Debug.Assert(_occupiedChunks.Count == 1);
        var takenStart = _occupiedChunks.GetKeyAtIndex(0);
        int rentStart;
        if (takenStart >= length)
        {
            takenStart -= length;
            rentStart = takenStart;
            _occupiedChunks.SetKeyAtIndex(0, takenStart);
        }
        else
        {
            var takenEnd = _occupiedChunks.GetValueAtIndex(0);
            rentStart = takenEnd;
            takenEnd += length;
            if (takenEnd > _activeBuffer.Length) return RentOnNewBuffer(length);

            Debug.Assert(_occupiedChunks.GetKeyAtIndex(0) == takenStart);
            _occupiedChunks.SetValueAtIndex(0, takenEnd);
        }

        return CreateWordBuf(rentStart, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Buffer<nuint> FastRentFromStart(int length)
    {
        _occupiedChunks.AddAssumeOrdered(0, length);
        return CreateWordBuf(0, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Buffer<nuint> CreateWordBuf(int start, int length)
    {
        _rentedBufCount++;
        _rentedWordCount += length;
        var span = MemoryMarshal.CreateSpan(
            ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_activeBuffer), start), length);
        return new Buffer<nuint>(start, span, this, _activeBuffer, length);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Release<T>(Buffer<T> buffer)
    {
        if (ClearUponReturn||RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            buffer.Span.Clear();
        if (!ReferenceEquals(_activeBuffer,buffer._src)) return;

        _rentedBufCount--;
        var bufLen = buffer._wordCount;
        _rentedWordCount -= bufLen;
        // if (_rentedCount == 0)
        // {
        //     _occupiedChunks.Clear();
        //     return;
        // }

        var bufStart = buffer._wordStart;
        var bufEnd = bufStart + bufLen;
        if (_occupiedChunks.Count == 1)
        {
            FastPathRemove(bufStart, bufEnd);
            return;
        }

        var bufEndIdx = _occupiedChunks.IndexOfValue(bufEnd);
        if (bufEndIdx != -1)
        {
            _occupiedChunks.SetValueAtIndex(bufEndIdx, bufStart);
            return;
        }

        var bufStartIdx = _occupiedChunks.IndexOfKey(bufStart);
        if (bufStartIdx != -1)
        {
            var chunkEnd = _occupiedChunks.GetValueAtIndex(bufStartIdx);
            var subset = chunkEnd != bufEnd;

            if (subset) _occupiedChunks.SetKeyAtIndex(bufStartIdx, bufEnd);
            else _occupiedChunks.RemoveAt(bufStartIdx);
            return;
        }


        RemoveBufFromOuter(bufStart, bufEnd);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FastPathRemove(int bufStart, int bufEnd)
    {
        Debug.Assert(_occupiedChunks.Count == 1);
        var outerStart = _occupiedChunks.GetKeyAtIndex(0);
        var outerEnd = _occupiedChunks.GetValueAtIndex(0);
        var startOverlap = bufStart == outerStart;
        var endOverlap = bufEnd == outerEnd;
        switch (startOverlap, endOverlap)
        {
            case (true, true):
                _occupiedChunks.Clear();
                break;
            case (true, false):
                _occupiedChunks.SetKeyAtIndex(0, bufEnd);
                break;
            case (false, true):
                _occupiedChunks.SetValueAtIndex(0, bufStart);
                break;
            case (false, false):
                _occupiedChunks.SetValueAtIndex(0, bufStart);
                _occupiedChunks.AddAssumeOrdered(bufEnd, outerEnd);
                break;
        }
    }   

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveBufFromOuter(int bufStart, int bufEnd)
    {
        var outerChunk = FindContainingChunk(bufStart);
        var index = outerChunk;

        var firstChunkEnd = bufStart;
        var secondChunkStart = bufEnd;
        var outerChunkEnd = _occupiedChunks.GetValueAtIndex(index);
        _occupiedChunks.SetValueAtIndex(index, firstChunkEnd);
        Debug.Assert(outerChunkEnd != bufEnd);
        _occupiedChunks.Insert(outerChunk + 1, secondChunkStart, outerChunkEnd);
    }

    private int FindContainingChunk(int bufStart)
    {
        var keys = _occupiedChunks.Keys;
        var values = _occupiedChunks.Values;

        Debug.Assert(keys.Length > 0);

        var low = 0;
        var high = keys.Length - 1;
        while (low <= high)
        {
            var mid = (low + high) >>> 1;
            var start = keys[mid];


            if (bufStart < start) high = mid - 1;
            else if (bufStart >= (values[mid])) low = mid + 1;
            else return mid;
        }

        return ThrowHelper.ThrowInvalidOperationException<int>(
            $"No containing chunk found for bufStart={bufStart}");
    }
}