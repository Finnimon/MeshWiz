using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.Buffers;

public sealed partial class Freelist
{
    

    public static int GrowGreedy<T>(in Buffer<T> buf)
    {
        if (buf is not { _alive: true, _wordCount: not 0 }) return 0;
        var alloc = buf._allocator;
        WeakSortedList occupiedChunks;
        object activeBuffer;
        int activeBufferLen;
        var maxWords = GetMaxWordCount<T>();
        bool managed;
        ref int rentedWordCount = ref Unsafe.NullRef<int>();
        if (typeof(T).IsValueType)
        {
            activeBuffer = alloc._activeUnmanagedBuffer;
            activeBufferLen = alloc._activeUnmanagedBuffer.Length;
            occupiedChunks = alloc._occupiedUnmanagedChunks;
            rentedWordCount = ref alloc._rentedUnmanagedWordCount;
            managed = false;
        }
        else
        {
            activeBuffer = alloc._activeManagedBuffer;
            activeBufferLen = alloc._activeManagedBuffer.Length;
            occupiedChunks = alloc._occupiedManagedChunks;
            rentedWordCount = ref alloc._rentedManagedElemCount;
            managed = true;
        }

        var previous = buf._wordCount;
        var growth = GrowGreedyImp(buf._wordStart, ref Unsafe.AsRef(in buf._wordCount), buf._src, activeBuffer,
            activeBufferLen, maxWords, occupiedChunks);
        if (!growth) return 0;
        var newSize = buf._wordCount;
        rentedWordCount += newSize - previous;
        Unsafe.AsRef(in buf.Span) = ResizeSpan(buf.Span, newSize, managed, out var elemDelta);
        return elemDelta;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Span<T> ResizeSpan<T>(Span<T> span, int wordCount, bool managed, out int delta)
    {
        var newSize = wordCount;
        if (!managed) newSize = (int)((16 * ((long)newSize)) / (long)Unsafe.SizeOf<T>());
        delta = newSize - span.Length;
        return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(span), newSize);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Span<T> ResizeSpanDirect<T>(Span<T> span, int elemCount)
    {
        return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(span), elemCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool GrowGreedyImp(int wordStart,
        ref int wordCount,
        object srcArr,
        object activeBuffer,
        int activeBufferLen,
        int maxWords,
        WeakSortedList occupiedChunks)
    {
        if (!ReferenceEquals(srcArr, activeBuffer)) return false;
        var bufEnd = wordCount + wordStart;
        if (bufEnd == activeBufferLen) return false;
        var bufIndex = occupiedChunks.IndexOfValue(bufEnd);
        if (bufIndex == -1) return false;

        if (bufIndex != occupiedChunks.Count - 1)
            return GreedyGapGrow(wordStart, ref wordCount, occupiedChunks, bufIndex, maxWords);
        var space = activeBufferLen - wordStart;
        var targetSize = int.Min(maxWords, space);
        if (targetSize <= wordCount) return false;
        wordCount = targetSize;
        occupiedChunks.SetValueAtIndex(bufIndex, targetSize + wordStart);
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool GreedyGapGrow(int wordStart,
        ref int wordCount,
        WeakSortedList occupiedChunks,
        int bufIndex,
        int maxWords)
    {
        var nextChunkIndex = bufIndex + 1;
        var nextChunkStart = occupiedChunks.GetKeyAtIndex(nextChunkIndex);
        var space = nextChunkStart - wordStart;
        var targetSize = int.Min(maxWords, space);
        if (targetSize <= wordCount) return false;
        wordCount = targetSize;
        var chunkValue = targetSize + wordStart;
        if (targetSize == space)
        {
            chunkValue = occupiedChunks.GetValueAtIndex(nextChunkIndex);
            occupiedChunks.RemoveAt(nextChunkIndex);
        }

        occupiedChunks.SetValueAtIndex(bufIndex, chunkValue);
        return true;
    }

    private static int GetMaxWordCount<T>()
    {
        if (!typeof(T).IsValueType) return Array.MaxLength;
        var sizeT = Unsafe.SizeOf<T>();
        return sizeT >= 16 ? Array.MaxLength : Utilities.GetWordCount<T>(Array.MaxLength);
    }
}