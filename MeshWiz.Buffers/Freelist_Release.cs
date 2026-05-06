using System.Diagnostics;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.Buffers;

public sealed partial class Freelist
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Release<T>(Buffer<T> buffer)
    {
        ref var rentedBufCount = ref Unsafe.NullRef<int>();
        ref var rentedElemCount = ref Unsafe.NullRef<int>();
        var bufLen = buffer._wordCount;
        var bufStart = buffer._wordStart;
        var bufEnd = bufStart + bufLen;
        var srcBuffer = buffer._src;
        WeakSortedList occupiedChunks;
        object activeBuffer;
        bool forceClear;
        if (typeof(T).IsValueType)
        {
            rentedBufCount = ref _rentedUnmanagedBufCount;
            rentedElemCount = ref _rentedUnmanagedWordCount;
            occupiedChunks = _occupiedUnmanagedChunks;
            activeBuffer = _activeUnmanagedBuffer;
            forceClear = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
        }
        else
        {
            rentedBufCount = ref _rentedManagedBufCount;
            rentedElemCount = ref _rentedManagedElemCount;
            occupiedChunks = _occupiedManagedChunks;
            activeBuffer = _activeManagedBuffer;
            forceClear = true;
        }

        if (forceClear || buffer._allocator.ClearUponReturn) buffer.Span.Clear();
        ReleaseAny(ref rentedBufCount,
            ref rentedElemCount,
            bufLen,
            occupiedChunks,
            activeBuffer,
            srcBuffer,
            bufStart,
            bufEnd);
    }

    private static int FindContainingChunk(int bufStart, WeakSortedList l)
    {
        var keys = l.Keys;
        var values = l.Values;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RemoveBufFromOuter(int bufStart, int bufEnd, WeakSortedList l)
    {
        var outerChunk = FindContainingChunk(bufStart, l);
        var index = outerChunk;

        var firstChunkEnd = bufStart;
        var secondChunkStart = bufEnd;
        var outerChunkEnd = l.GetValueAtIndex(index);
        l.SetValueAtIndex(index, firstChunkEnd);
        Debug.Assert(outerChunkEnd != bufEnd);
        l.Insert(outerChunk + 1, secondChunkStart, outerChunkEnd);
    }


    private static void ReleaseAny(ref int rentedBufCount, ref int rentedElemCount, int bufLen,
        WeakSortedList occupiedChunks, object activeBuffer, object srcBuffer,
        int bufStart, int bufEnd)
    {
        if (!ReferenceEquals(activeBuffer, srcBuffer)) return;

        rentedBufCount--;
        rentedElemCount -= bufLen;
        if (rentedBufCount == 0)
        {
            occupiedChunks.Clear();
            return;
        }

        if (occupiedChunks.Count == 1)
        {
            FastPathRemove(bufStart, bufEnd, occupiedChunks);
            return;
        }

        SlowRemove(bufStart, bufEnd, occupiedChunks);
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SlowRemove(int bufStart, int bufEnd, WeakSortedList l)
    {
        var bufEndIdx = l.IndexOfValue(bufEnd);
        if (bufEndIdx != -1)
        {
            l.SetValueAtIndex(bufEndIdx, bufStart);
            return;
        }

        var bufStartIdx = l.IndexOfKey(bufStart);
        if (bufStartIdx != -1)
        {
            var chunkEnd = l.GetValueAtIndex(bufStartIdx);
            var subset = chunkEnd != bufEnd;

            if (subset) l.SetKeyAtIndex(bufStartIdx, bufEnd);
            else l.RemoveAt(bufStartIdx);
            return;
        }


        RemoveBufFromOuter(bufStart, bufEnd, l);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FastPathRemove(int bufStart, int bufEnd, WeakSortedList l)
    {
        Debug.Assert(l.Count == 1);
        var outerStart = l.GetKeyAtIndex(0);
        var outerEnd = l.GetValueAtIndex(0);
        var startOverlap = bufStart == outerStart;
        var endOverlap = bufEnd == outerEnd;
        switch (startOverlap, endOverlap)
        {
            case (true, true):
                l.Clear();
                break;
            case (true, false):
                l.SetKeyAtIndex(0, bufEnd);
                break;
            case (false, true):
                l.SetValueAtIndex(0, bufStart);
                break;
            case (false, false):
                l.SetValueAtIndex(0, bufStart);
                l.AddAssumeOrdered(bufEnd, outerEnd);
                break;
        }
    }
}