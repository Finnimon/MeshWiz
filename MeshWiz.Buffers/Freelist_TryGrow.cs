using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.Buffers;

public sealed partial class Freelist
{
    public static bool TryGrow<T>(in Buffer<T> buf, int minGrowth)
    {
        if (minGrowth < 0) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(minGrowth));
        if (minGrowth == 0) return true;
        if (buf is not { _alive: true, _wordCount: not 0 }) return false;
        var alloc = buf._allocator;
        WeakSortedList occupiedChunks;
        object activeBuffer;
        int activeBufferLen;
        var maxWords = GetMaxWordCount<T>();
        ref var rentedWordCount = ref Unsafe.NullRef<int>();
        var targetSpanLen = buf.Span.Length + minGrowth;
        int targetWordCount;
        if (typeof(T).IsValueType)
        {
            activeBuffer = alloc._activeUnmanagedBuffer;
            activeBufferLen = alloc._activeUnmanagedBuffer.Length;
            occupiedChunks = alloc._occupiedUnmanagedChunks;
            rentedWordCount = ref alloc._rentedUnmanagedWordCount;
            targetWordCount = Utilities.GetWordCount<T>(targetSpanLen);
            targetSpanLen = Utilities.GetElemCount<T>(targetWordCount);
        }
        else
        {
            activeBuffer = alloc._activeManagedBuffer;
            activeBufferLen = alloc._activeManagedBuffer.Length;
            occupiedChunks = alloc._occupiedManagedChunks;
            rentedWordCount = ref alloc._rentedManagedElemCount;
            targetWordCount = targetSpanLen;
        }

        if (!ReferenceEquals(buf._src, activeBuffer)) return false;

        var bufEnd = buf._wordCount + buf._wordStart;
        var targetEnd = buf._wordStart + targetWordCount;
        var wordCountDelta = targetWordCount - buf._wordCount;
        if (activeBufferLen < targetEnd) return false;
        var bufIndex = occupiedChunks.IndexOfValue(bufEnd);
        if (bufIndex == -1) return false;
        var isFinalChunk = bufIndex == occupiedChunks.Count - 1;
        if (isFinalChunk)
        {
            rentedWordCount += wordCountDelta;
            occupiedChunks.SetValueAtIndex(bufIndex, targetEnd);
            Unsafe.AsRef(in buf.Span) = ResizeSpanDirect(buf.Span, targetSpanLen);
            return true;
        }

        var nextChunkIndex = bufIndex + 1;
        var nextChunkStart = occupiedChunks.GetKeyAtIndex(nextChunkIndex);
        if (nextChunkStart < targetEnd) return false;
        Unsafe.AsRef(in buf.Span) = ResizeSpanDirect(buf.Span, targetSpanLen);
        if (nextChunkStart == targetEnd)
        {
            targetEnd = occupiedChunks.GetValueAtIndex(nextChunkIndex);
            occupiedChunks.RemoveAt(nextChunkIndex);
        }

        occupiedChunks.SetValueAtIndex(bufIndex, targetEnd);
        rentedWordCount += wordCountDelta;
        return true;
    }

    //
    // /// <summary>
    // /// 
    // /// </summary>
    // /// <param name="buf"></param>
    // /// <param name="growBy"></param>
    // /// <typeparam name="T"></typeparam>
    // /// <exception cref="ArgumentOutOfRangeException">when <paramref name="growBy"/> is less than 0</exception>
    // /// <returns></returns>
    // public static bool TryGrow<T>(in Buffer<T> buf, int growBy)
    // {
    //     if (growBy < 0) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(growBy));
    //     if (!buf._alive) return false;
    //     if (growBy == 0) return true;
    //     var alloc = buf._allocator;
    //     var activeBuffer = alloc._activeUnmanagedBuffer;
    //     if (!ReferenceEquals(activeBuffer, buf._src)) return false;
    //
    //     var totalLen = buf.Span.Length + growBy;
    //     var totalWordCount = Utilities.GetWordCount<T>(totalLen);
    //     var bufEnd = buf._wordCount + buf._wordStart;
    //     var targetBufEnd = totalWordCount + buf._wordStart;
    //     var totalMemLength = activeBuffer.Length;
    //
    //     if (targetBufEnd > totalMemLength) return false;
    //
    //     var chunkCount = alloc._occupiedUnmanagedChunks.Count;
    //     Debug.Assert(chunkCount > 0);
    //     var lastIndex = chunkCount - 1;
    //     var isLastChunk = alloc._occupiedUnmanagedChunks.GetValueAtIndex(lastIndex) == bufEnd;
    //     if (isLastChunk)
    //         return FastGrowFinalChunk(in buf,
    //             alloc,
    //             totalLen,
    //             totalWordCount,
    //             lastIndex);
    //     return SlowGrow(in buf,
    //         alloc,
    //         bufEnd,
    //         targetBufEnd,
    //         totalLen,
    //         totalWordCount);
    // }
    //
    // [MethodImpl(MethodImplOptions.NoInlining)]
    // private static bool SlowGrow<T>(in Buffer<T> buf, Freelist alloc, int bufEnd, int targetBufEnd,
    //     int totalLen,
    //     int totalWordCount)
    // {
    //     var chunkIndex = alloc._occupiedUnmanagedChunks.IndexOfValue(bufEnd);
    //     if (chunkIndex == -1) return false;
    //     var nextChunkIndex = chunkIndex + 1;
    //     var nextStart = alloc._occupiedUnmanagedChunks.GetKeyAtIndex(nextChunkIndex);
    //
    //     if (nextStart < targetBufEnd) return false;
    //
    //     if (nextStart != targetBufEnd)
    //     {
    //         Resize(in buf, alloc, totalLen, totalWordCount, chunkIndex);
    //         return true;
    //     }
    //
    //     ResizeMerging(in buf, alloc, nextChunkIndex, chunkIndex, totalLen, totalWordCount);
    //     return true;
    // }
    //
    // private static void ResizeMerging<T>(in Buffer<T> buf, Freelist alloc, int nextChunkIndex, int chunkIndex,
    //     int newElemCount, int newWordCount)
    // {
    //     var nextChunkEnd = alloc._occupiedUnmanagedChunks.GetValueAtIndex(nextChunkIndex);
    //     alloc._occupiedUnmanagedChunks.RemoveAt(nextChunkIndex);
    //     alloc._occupiedUnmanagedChunks.SetValueAtIndex(chunkIndex, nextChunkEnd);
    //     alloc._rentedUnmanagedWordCount += newWordCount - buf._wordCount;
    //
    //     Unsafe.AsRef(in buf._wordCount) = newWordCount;
    //     Unsafe.AsRef(in buf.Span) = Utilities.Resize(buf.Span, newElemCount);
    // }
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // private static bool FastGrowFinalChunk<T>(in Buffer<T> buf, Freelist alloc, int newElemCount,
    //     int newWordCount, int lastIndex)
    // {
    //     Resize(in buf, alloc, newElemCount, newWordCount, lastIndex);
    //     return true;
    // }
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // private static void Resize<T>(in Buffer<T> buf, Freelist alloc, int newElemCount, int newWordCount,
    //     int containingChunkIndex)
    // {
    //     alloc._occupiedUnmanagedChunks.SetValueAtIndex(containingChunkIndex, buf._wordStart + newWordCount);
    //     alloc._rentedUnmanagedWordCount += newWordCount - buf._wordCount;
    //     Unsafe.AsRef(in buf._wordCount) = newWordCount;
    //     Unsafe.AsRef(in buf.Span) = Utilities.Resize(buf.Span, newElemCount);
    // }
}