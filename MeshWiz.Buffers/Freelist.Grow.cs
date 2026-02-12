using System.Diagnostics;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.Buffers;

public sealed partial class Freelist
{
    public static int GrowGreedy<T>(in Buffer<T> buf)
    {
        var alloc = buf._allocator;
        if (!buf._alive) ThrowHelper.ThrowInvalidOperationException("Dead buffer.");
        if (!ReferenceEquals(alloc._activeBuffer,buf._src)) return 0;
        var bufEnd = buf._wordStart + buf._wordCount;

        var bufIndex = alloc._occupiedChunks.IndexOfValue(bufEnd);
        if (bufIndex == -1) return 0;
        var lastIndex = alloc._occupiedChunks.Count - 1;
        var allocEnd = alloc._activeBuffer.Length;
        if (bufIndex == lastIndex)
            return GreedyGrowFromEnd(in buf, allocEnd, alloc, bufIndex);

        return GreedyGapGrow(in buf, bufIndex, alloc);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int GreedyGapGrow<T>(in Buffer<T> buf, int bufIndex, Freelist alloc)
    {
        var nextChunkIndex = bufIndex + 1;
        var nextChunkStart = alloc._occupiedChunks.GetKeyAtIndex(nextChunkIndex);
        var space = nextChunkStart - buf._wordStart;
        var maxWords = GetMaxWordCount<T>();
        var targetSize = int.Min(maxWords, space);
        var newElemCount = Utilities.GetElemCount<T>(targetSize);
        var elemGrowth = newElemCount - buf.Span.Length;
        if (elemGrowth <= 0) return 0;
        if (targetSize == space)
        {
            ResizeMerging(in buf, alloc, nextChunkIndex, bufIndex, newElemCount, targetSize);
            return elemGrowth;
        }

        Resize(in buf,
            alloc,
            newElemCount,
            targetSize,
            bufIndex);
        return elemGrowth;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GreedyGrowFromEnd<T>(in Buffer<T> buf, int allocEnd, Freelist alloc, int bufIndex)
    {
        var space = allocEnd - buf._wordStart;
        var maxWords = GetMaxWordCount<T>();
        var targetSize = int.Min(maxWords, space);
        if (targetSize <= buf._wordCount) return 0;
        var newElemCount = Utilities.GetElemCount<T>(targetSize);
        var growth = newElemCount - buf.Span.Length;
        Resize(in buf,
            alloc,
            newElemCount,
            targetSize,
            bufIndex);
        return growth;
    }

    private static int GetMaxWordCount<T>()
    {
        var sizeT = Unsafe.SizeOf<T>();
        return sizeT >= 16 ? Array.MaxLength : Utilities.GetWordCount<T>(Array.MaxLength);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buf"></param>
    /// <param name="growBy"></param>
    /// <typeparam name="T"></typeparam>
    /// <exception cref="ArgumentOutOfRangeException">when <paramref name="growBy"/> is less than 0</exception>
    /// <returns></returns>
    public static bool TryGrow<T>(in Buffer<T> buf, int growBy)
    {
        if (growBy < 0) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(growBy));
        if (!buf._alive) return false;
        if (growBy == 0) return true;
        var alloc = buf._allocator;
        var activeBuffer = alloc._activeBuffer;
        if (!ReferenceEquals(activeBuffer,buf._src)) return false;

        var totalLen = buf.Span.Length + growBy;
        var totalWordCount = Utilities.GetWordCount<T>(totalLen);
        var bufEnd = buf._wordCount + buf._wordStart;
        var targetBufEnd = totalWordCount + buf._wordStart;
        var totalMemLength = activeBuffer.Length;

        if (targetBufEnd > totalMemLength) return false;

        var chunkCount = alloc._occupiedChunks.Count;
        Debug.Assert(chunkCount > 0);
        var lastIndex = chunkCount - 1;
        var isLastChunk = alloc._occupiedChunks.GetValueAtIndex(lastIndex) == bufEnd;
        if (isLastChunk)
            return FastGrowFinalChunk(in buf,
                alloc,
                totalLen,
                totalWordCount,
                lastIndex);
        return SlowGrow(in buf,
            alloc,
            bufEnd,
            targetBufEnd,
            totalLen,
            totalWordCount);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool SlowGrow<T>(in Buffer<T> buf, Freelist alloc, int bufEnd, int targetBufEnd,
        int totalLen,
        int totalWordCount)
    {
        var chunkIndex = alloc._occupiedChunks.IndexOfValue(bufEnd);
        if (chunkIndex == -1) return false;
        var nextChunkIndex = chunkIndex + 1;
        var nextStart = alloc._occupiedChunks.GetKeyAtIndex(nextChunkIndex);

        if (nextStart < targetBufEnd) return false;

        if (nextStart != targetBufEnd)
        {
            Resize(in buf, alloc, totalLen, totalWordCount, chunkIndex);
            return true;
        }

        ResizeMerging(in buf, alloc, nextChunkIndex, chunkIndex, totalLen, totalWordCount);
        return true;
    }

    private static void ResizeMerging<T>(in Buffer<T> buf, Freelist alloc, int nextChunkIndex, int chunkIndex,
        int newElemCount, int newWordCount)
    {
        var nextChunkEnd = alloc._occupiedChunks.GetValueAtIndex(nextChunkIndex);
        alloc._occupiedChunks.RemoveAt(nextChunkIndex);
        alloc._occupiedChunks.SetValueAtIndex(chunkIndex, nextChunkEnd);
        alloc._rentedWordCount += newWordCount - buf._wordCount;

        Unsafe.AsRef(in buf._wordCount) = newWordCount;
        Unsafe.AsRef(in buf.Span)=Utilities.Resize(buf.Span, newElemCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool FastGrowFinalChunk<T>(in Buffer<T> buf, Freelist alloc, int newElemCount,
        int newWordCount, int lastIndex)
    {
        Resize(in buf, alloc, newElemCount, newWordCount, lastIndex);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Resize<T>(in Buffer<T> buf, Freelist alloc, int newElemCount, int newWordCount,
        int containingChunkIndex)
    {
        alloc._occupiedChunks.SetValueAtIndex(containingChunkIndex, buf._wordStart + newWordCount);
        alloc._rentedWordCount += newWordCount - buf._wordCount;
        Unsafe.AsRef(in buf._wordCount) = newWordCount;
        Unsafe.AsRef(in buf.Span)=Utilities.Resize(buf.Span, newElemCount);
    }
}