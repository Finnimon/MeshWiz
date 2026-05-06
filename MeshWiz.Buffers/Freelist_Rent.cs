using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using JetBrains.Annotations;

namespace MeshWiz.Buffers;

public sealed partial class Freelist
{

    [MustUseReturnValue, MustDisposeResource, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Buffer<T> Rent<T>(int minimumLength)
    {
        if (minimumLength < 0) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(minimumLength));
        if (minimumLength == 0) return EmptyBuffer<T>();
        return typeof(T).IsValueType
            ? RentUnmanaged<T>(minimumLength)
            : RentManaged<T>(minimumLength);
    }

    private Buffer<T> RentUnmanaged<T>(int elemCount)
    {
        var length = Utilities.GetWordCount<T>(elemCount);
        return Buffer<T>.FromWordBuf(CreateAnyBuffer(length, _occupiedUnmanagedChunks, ref _rentedUnmanagedBufCount,
            ref _rentedUnmanagedWordCount, _activeUnmanagedBuffer, this));
    }

    private Buffer<T> RentManaged<T>(int len) =>
        Buffer<T>.FromObjectBuf(CreateAnyBuffer(len,
            _occupiedManagedChunks,
            ref _rentedManagedBufCount,
            ref _rentedManagedElemCount,
            _activeManagedBuffer,
            this));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Buffer<T> CreateAnyBuffer<T>(int length,
        WeakSortedList occupiedChunks,
        ref int rentedBufCount,
        ref int rentedElemCount,
        T[] activeBuffer,
        Freelist allocator)
    {
        if (length <= (activeBuffer.Length - rentedElemCount))
        {
            if (rentedBufCount == 0)
                return FastRentFromStart(length, occupiedChunks, ref rentedBufCount, ref rentedElemCount, activeBuffer,
                    allocator);
            if (occupiedChunks.Count == 1)
                return FastDenseRent(length, occupiedChunks, ref rentedBufCount, ref rentedElemCount, activeBuffer,
                    allocator);
            return FindGapRent(length, occupiedChunks, ref rentedBufCount, ref rentedElemCount, activeBuffer,
                allocator);
        }

        return allocator.RentOnNewBuffer<T>(length);
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Buffer<T> FindGapRent<T>(int len,
        WeakSortedList occupiedChunks,
        ref int rentedBufCount,
        ref int rentedElemCount,
        T[] activeBuffer,
        Freelist allocator)
    {
        var chunkCount = occupiedChunks.Count;
        var lastIndex = chunkCount - 1;
        var totalEnd = occupiedChunks.GetValueAtIndex(lastIndex);
        var totalGapsSize = totalEnd - rentedBufCount;
        var gapCount = lastIndex;
        var maxGapSize = totalGapsSize - gapCount + 1;
        var maybeGapPossible = maxGapSize >= len;
        if (!maybeGapPossible)
        {
            var rem = activeBuffer.Length - totalEnd;
            if (rem >= len)
                return RentAfter(lastIndex, len, totalEnd, occupiedChunks, ref rentedBufCount, ref rentedElemCount,
                    activeBuffer, allocator);
            return allocator.RentOnNewBuffer<T>(len);
        }

        KeyValuePair<int, int> previous = default;
        var pos = 0;
        for (var i = 0; i < chunkCount; i++)
        {
            var chunk = occupiedChunks.GetEntryAtIndex(i);
            var gapSize = chunk.Key - previous.Value;
            if (gapSize > len || i == 0 && gapSize == len)
                return RentBefore(i, len, chunk, occupiedChunks, ref rentedBufCount, ref rentedElemCount, activeBuffer,
                    allocator);
            if (gapSize == len)
                return RentBetween(i - 1, i, len, previous, chunk, occupiedChunks, ref rentedBufCount,
                    ref rentedElemCount, activeBuffer, allocator);
            if (++pos == chunkCount && len <= activeBuffer.Length - chunk.Value)
                return RentAfter(i, len, chunk.Value, occupiedChunks, ref rentedBufCount, ref rentedElemCount,
                    activeBuffer, allocator);
            previous = chunk;
        }

        return allocator.RentOnNewBuffer<T>(len);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Buffer<T> RentOnNewBuffer<T>(int length)
    {
#if DEBUG
        if (typeof(T) != typeof(object) && typeof(T) != typeof(UInt128)) throw new Exception("Invalid type");
#endif
        WeakSortedList occupiedChunks;
        ref var rentedBufCount = ref Unsafe.NullRef<int>();
        ref var rentedElemCount = ref Unsafe.NullRef<int>();
        T[] activeBuffer;
        if (typeof(T).IsValueType)
        {
            occupiedChunks = _occupiedUnmanagedChunks;
            var nextSize = NextAllocSize(_activeUnmanagedBuffer.Length, length,_initialUnmanagedWordCount);
            activeBuffer = (T[])(object)(_activeUnmanagedBuffer = GC.AllocateUninitializedArray<UInt128>(nextSize));

            rentedBufCount = ref _rentedUnmanagedBufCount;
            rentedElemCount = ref _rentedUnmanagedWordCount;
        }
        else
        {
            occupiedChunks = _occupiedManagedChunks;
            var nextSize = NextAllocSize(_activeManagedBuffer.Length, length,_initialManagedWordCount);
            activeBuffer = (T[])(object)(_activeManagedBuffer = new object[nextSize]);
            rentedBufCount = ref _rentedManagedBufCount;
            rentedElemCount = ref _rentedManagedElemCount;
        }

        rentedBufCount = 0;
        rentedElemCount = 0;
        occupiedChunks.Clear();

        return FastRentFromStart(length, occupiedChunks, ref rentedBufCount, ref rentedElemCount, activeBuffer, this);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Buffer<T> RentBetween<T>(int chunkBefore,
        int chunkAfter,
        int len,
        KeyValuePair<int, int> previous,
        KeyValuePair<int, int> chunk,
        WeakSortedList occupiedChunks,
        ref int rentedBufCount,
        ref int rentedElemCount,
        T[] activeBuffer,
        Freelist allocator)

    {
        var bufStart = previous.Value;
        var chunkEnd = chunk.Key;
        occupiedChunks.RemoveAt(chunkAfter);
        occupiedChunks.SetValueAtIndex(chunkBefore, chunkEnd);
        return CreateBuf(bufStart, len, ref rentedBufCount, ref rentedElemCount, activeBuffer, allocator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Buffer<T> RentBefore<T>(int index,
        int length,
        KeyValuePair<int, int> chunk,
        WeakSortedList occupiedChunks,
        ref int rentedBufCount,
        ref int rentedElemCount,
        T[] activeBuffer,
        Freelist allocator)
    {
        var start = chunk.Key;
        var bufStart = start - length;
        occupiedChunks.SetKeyAtIndex(index, bufStart);
        return CreateBuf(bufStart, length, ref rentedBufCount, ref rentedElemCount, activeBuffer, allocator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Buffer<T> RentAfter<T>(int index,
        int length,
        int oldEnd,
        WeakSortedList occupiedChunks,
        ref int rentedBufCount,
        ref int rentedElemCount,
        T[] activeBuffer,
        Freelist allocator)
    {
        var newEnd = oldEnd + length;
        occupiedChunks.SetValueAtIndex(index, newEnd);
        return CreateBuf(oldEnd, length, ref rentedBufCount, ref rentedElemCount, activeBuffer, allocator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Buffer<T> FastDenseRent<T>(int length,
        WeakSortedList occupiedChunks,
        ref int rentedBufCount,
        ref int rentedElemCount,
        T[] activeBuffer,
        Freelist allocator)
    {
        Debug.Assert(occupiedChunks.Count == 1);
        var takenStart = occupiedChunks.GetKeyAtIndex(0);
        int rentStart;
        if (takenStart >= length)
        {
            takenStart -= length;
            rentStart = takenStart;
            occupiedChunks.SetKeyAtIndex(0, takenStart);
        }
        else
        {
            var takenEnd = occupiedChunks.GetValueAtIndex(0);
            rentStart = takenEnd;
            takenEnd += length;
            if (takenEnd > activeBuffer.Length) return allocator.RentOnNewBuffer<T>(length);

            Debug.Assert(occupiedChunks.GetKeyAtIndex(0) == takenStart);
            occupiedChunks.SetValueAtIndex(0, takenEnd);
        }

        return CreateBuf(rentStart, length, ref rentedBufCount, ref rentedElemCount, activeBuffer, allocator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Buffer<T> FastRentFromStart<T>(int length,
        WeakSortedList occupiedChunks,
        ref int rentedBufCount,
        ref int rentedElemCount,
        T[] activeBuffer,
        Freelist allocator)
    {
        occupiedChunks.AddAssumeOrdered(0, length);
        return CreateBuf(0, length, ref rentedBufCount, ref rentedElemCount, activeBuffer, allocator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Buffer<T> CreateBuf<T>(int start,
        int length,
        ref int rentedBufCount,
        ref int rentedElemCount,
        T[] activeBuffer,
        Freelist allocator)
    {
        rentedBufCount++;
        rentedElemCount += length;
        var span = MemoryMarshal.CreateSpan(
            ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(activeBuffer), start),
            length);
        return new Buffer<T>(start, span, allocator, activeBuffer, length);
    }
}