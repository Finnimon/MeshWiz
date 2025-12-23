// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Modifications: Replaced IEnumerable with IEnumerator and ReadOnlySpan
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MeshWiz.RefLinq;

/// <summary>Provides a helper for efficiently building arrays and lists.</summary>
/// <remarks>This is implemented as an inline array of rented arrays.</remarks>
/// <typeparam name="T">Specifies the element type of the collection being built.</typeparam>
internal ref struct SegmentedArrayBuilder<T>
{
    /// <summary>The size to use for the first segment that's stack allocated by the caller.</summary>
    /// <remarks>
    /// This value needs to be small enough that we don't need to be overly concerned about really large
    /// value types. It's not unreasonable for a method to say it has 8 locals of a T, and that's effectively
    /// what this is.
    /// </remarks>
    private const int ScratchBufferSize = 8;

    /// <summary>Minimum size to request renting from the pool.</summary>
    private const int MinimumRentSize = 16;

    /// <summary>The array of segments.</summary>
    /// <remarks><see cref="_segmentsCount"/> is how many of the segments are valid in <see cref="_segments"/>, not including <see cref="_firstSegment"/>.</remarks>
    private Arrays _segments;

    /// <summary>The scratch buffer provided by the caller.</summary>
    /// <remarks>This is treated as the initial segment, before anything in <see cref="_segments"/>.</remarks>
    private Span<T> _firstSegment;

    /// <summary>The current span. This points either to <see cref="_firstSegment"/> or to <see cref="_segments"/>[<see cref="_segmentsCount"/> - 1].</summary>
    private Span<T> _currentSegment;

    /// <summary>The count of segments in <see cref="_segments"/> that are valid.</summary>
    /// <remarks>All but the last are known to be fully populated.</remarks>
    private int _segmentsCount;

    /// <summary>The total number of elements in all but the current/last segment.</summary>
    private int _countInFinishedSegments;

    /// <summary>The number of elements in the current/last segment.</summary>
    private int _countInCurrentSegment;

    /// <summary>Initialize the builder.</summary>
    /// <param name="scratchBuffer">A buffer that can be used as part of the builder.</param>
    public SegmentedArrayBuilder(Span<T> scratchBuffer)
    {
        _currentSegment = _firstSegment = scratchBuffer;
    }

    /// <summary>Clean up the resources used by the builder.</summary>
    public void Dispose()
    {
        var segmentsCount = _segmentsCount;
        if (segmentsCount != 0)
        {
            ReturnArrays(segmentsCount);
        }
    }

    private void ReturnArrays(int segmentsCount)
    {
        ReadOnlySpan<T[]> segments = _segments;

        // We need to return all rented arrays to the pool, and if the arrays contain any references,
        // we want to clear them first so that the pool doesn't artificially root contained objects.
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            // Return all but the last segment. All of these are full and need to be entirely cleared.
            segmentsCount--;
            foreach (var segment in segments.Slice(0, segmentsCount))
            {
                Array.Clear(segment);
                ArrayPool<T>.Shared.Return(segment);
            }

            // For the last segment, we can clear only what we know was used.
            var currentSegment = segments[segmentsCount];
            Array.Clear(currentSegment, 0, _countInCurrentSegment);
            ArrayPool<T>.Shared.Return(currentSegment);
        }
        else
        {
            // Return every rented array without clearing.
            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                if (segment is null)
                {
                    break;
                }

                ArrayPool<T>.Shared.Return(segment);
            }
        }
    }

    /// <summary>Gets the number of elements in the builder.</summary>
    public readonly int Count => checked(_countInFinishedSegments + _countInCurrentSegment);

    /// <summary>Adds an item into the builder.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        var currentSegment = _currentSegment;
        var countInCurrentSegment = _countInCurrentSegment;
        if ((uint)countInCurrentSegment < (uint)currentSegment.Length)
        {
            currentSegment[countInCurrentSegment] = item;
            _countInCurrentSegment++;
        }
        else
        {
            AddSlow(item);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AddSlow(T item)
    {
        Expand();
        _currentSegment[0] = item;
        _countInCurrentSegment = 1;
    }

    /// <summary>Adds a collection of items into the builder.</summary>
    public void AddRange(ReadOnlySpan<T> sourceSpan)
    {
        var availableSpaceInCurrentSpan = _currentSegment.Length - _countInCurrentSegment;
        var sourceSlice = sourceSpan.Slice(0, Math.Min(availableSpaceInCurrentSpan, sourceSpan.Length));
        sourceSlice.CopyTo(_currentSegment.Slice(_countInCurrentSegment));
        _countInCurrentSegment += sourceSlice.Length;
        sourceSlice = sourceSpan.Slice(sourceSlice.Length);

        if (!sourceSlice.IsEmpty)
        {
            Expand(sourceSlice.Length);
            sourceSlice.CopyTo(_currentSegment);
            _countInCurrentSegment = sourceSlice.Length;
        }
    }

    /// <summary>Adds a collection of items into the builder.</summary>
    /// <remarks>
    /// The implementation assumes the caller has already ruled out the source being
    /// and ICollection and thus doesn't bother checking to see if it is.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AddNonICollectionRange<TIter>(TIter source)
        where TIter : IEnumerator<T>, allows ref struct
        => AddNonICollectionRangeInlined(source);

    /// <summary>Adds a collection of items into the builder.</summary>
    /// <remarks>
    /// The implementation assumes the caller has already ruled out the source being
    /// and ICollection and thus doesn't bother checking to see if it is.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddNonICollectionRangeInlined<TIter>(TIter source)
        where TIter : IEnumerator<T>, allows ref struct
    {
        if (source.TryConvertToSpanIter<TIter, T>(out var spanIterator))
        {
            AddRange(spanIterator.OriginalSource);
            return;
        }
        var currentSegment = _currentSegment;
        var countInCurrentSegment = _countInCurrentSegment;
        
        while (source.MoveNext())
        {
            var item = source.Current;
            if ((uint)countInCurrentSegment < (uint)currentSegment.Length)
            {
                currentSegment[countInCurrentSegment] = item;
                countInCurrentSegment++;
            }
            else
            {
                Expand();
                currentSegment = _currentSegment;
                currentSegment[0] = item;
                countInCurrentSegment = 1;
            }
        }

        _countInCurrentSegment = countInCurrentSegment;
    }

    /// <summary>Creates an array containing all of the elements in the builder.</summary>
    public readonly T[] ToArray()
    {
        T[] result;
        var count = Count;

        if (count != 0)
        {
            result = GC.AllocateUninitializedArray<T>(count);
            ToSpanInlined(result);
        }
        else
        {
            result = [];
        }

        return result;
    }

    /// <summary>Creates a list containing all of the elements in the builder.</summary>
    public readonly List<T> ToList()
    {
        List<T> result;
        var count = Count;

        if (count != 0)
        {
            result = new List<T>(count);

            CollectionsMarshal.SetCount(result, count);
            ToSpanInlined(CollectionsMarshal.AsSpan(result));
        }
        else
        {
            result = [];
        }

        return result;
    }

    /// <summary>Creates an array containing all of the elements in the builder.</summary>
    /// <param name="additionalLength">The number of extra elements of room to allocate in the resulting array.</param>
    public readonly T[] ToArray(int additionalLength)
    {
        T[] result;
        var count = checked(Count + additionalLength);

        if (count != 0)
        {
            result = GC.AllocateUninitializedArray<T>(count);
            ToSpanInlined(result);
        }
        else
        {
            result = [];
        }

        return result;
    }

    /// <summary>Populates the destination span with all of the elements in the builder.</summary>
    /// <param name="destination">The destination span.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public readonly void ToSpan(Span<T> destination) => ToSpanInlined(destination);

    /// <summary>Populates the destination span with all of the elements in the builder.</summary>
    /// <param name="destination">The destination span.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void ToSpanInlined(Span<T> destination)
    {
        var segmentsCount = _segmentsCount;
        if (segmentsCount != 0)
        {
            // Copy the first segment
            ReadOnlySpan<T> firstSegment = _firstSegment;
            firstSegment.CopyTo(destination);
            destination = destination.Slice(firstSegment.Length);

            // Copy the 0..N-1 segments
            segmentsCount--;
            if (segmentsCount != 0)
            {
                foreach (var arr in ((ReadOnlySpan<T[]>)_segments).Slice(0, segmentsCount))
                {
                    ReadOnlySpan<T> segment = arr;
                    segment.CopyTo(destination);
                    destination = destination.Slice(segment.Length);
                }
            }
        }

        // Copy the last segment
        _currentSegment.Slice(0, _countInCurrentSegment).CopyTo(destination);
    }

    /// <summary>Appends a new segment onto the builder.</summary>
    /// <param name="minimumRequired">The minimum amount of space to allocate in a new segment being appended.</param>
    private void Expand(int minimumRequired = MinimumRentSize)
    {
        if (minimumRequired < MinimumRentSize)
        {
            minimumRequired = MinimumRentSize;
        }

        // Update our count of the number of elements in the arrays.
        // If we know we're exceeding the maximum allowed array length, throw.
        var currentSegmentLength = _currentSegment.Length;
        checked
        {
            _countInFinishedSegments += currentSegmentLength;
        }

        if (_countInFinishedSegments > Array.MaxLength)
        {
            throw new OutOfMemoryException();
        }

        // Use a typical doubling algorithm to decide the length of the next array
        // and allocate it. We want to double the current array length, but if the
        // minimum required is larger than that, use the minimum required. And if
        // doubling would result in going above the max array length, only use the
        // max array length, as List<T> does.
        var newSegmentLength = (int)Math.Min(Math.Max(minimumRequired, currentSegmentLength * 2L), Array.MaxLength);
        _currentSegment = _segments[_segmentsCount] = ArrayPool<T>.Shared.Rent(newSegmentLength);
        _segmentsCount++;
    }

#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0051 // Remove unused private members
    /// <summary>A struct to hold all of the T[]s that compose the full result set.</summary>
    /// <remarks>
    /// Starting at the minimum size of <see cref="MinimumRentSize"/>, and with a minimum of doubling
    /// on every growth, this is large enough to hold the maximum number arrays that could result
    /// until the total length has exceeded Array.MaxLength.
    /// </remarks>
    [InlineArray(27)]
    private struct Arrays
    {
        private T[] _values;
    }

    /// <summary>Provides a stack-allocatable buffer for use as an argument to the builder.</summary>
    [InlineArray(ScratchBufferSize)]
    public struct ScratchBuffer
    {
        private T _item;
    }
#pragma warning restore IDE0051
#pragma warning restore IDE0044
}