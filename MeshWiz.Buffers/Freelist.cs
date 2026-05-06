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
    public readonly bool ClearUponReturn;
    private readonly int _initialUnmanagedWordCount;
    private readonly int _initialManagedWordCount;
    private object[] _activeManagedBuffer;
    private readonly WeakSortedList _occupiedManagedChunks;
    public bool IsManagedDense => _occupiedManagedChunks.Count == 1;
    public bool NoneManagedRented => _occupiedManagedChunks.Count == 0;
    public long ManagedCapacity =>  _activeManagedBuffer.LongLength;
    private int _rentedManagedBufCount;
    private int _rentedManagedElemCount;

    
    private UInt128[] _activeUnmanagedBuffer;
    private readonly WeakSortedList _occupiedUnmanagedChunks;
    public bool IsUnmanagedDense => _occupiedUnmanagedChunks.Count == 1;
    public bool NoneUnmanagedRented => _occupiedUnmanagedChunks.Count == 0;
    public long UnmanagedCapacity => 16 * (long)_activeUnmanagedBuffer.Length;
    private int _rentedUnmanagedBufCount;
    private int _rentedUnmanagedWordCount;


    public Freelist(long initialByteSize, bool clearUponReturn)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialByteSize, 0, nameof(initialByteSize));
        _initialUnmanagedWordCount = int.Max(1, Utilities.GetWordCount<byte>(initialByteSize));
        _initialManagedWordCount = int.Max(1, (int)(initialByteSize / nint.Size));
        _activeUnmanagedBuffer = [];
        _occupiedUnmanagedChunks = new WeakSortedList(32);
        _activeManagedBuffer = [];
        _occupiedManagedChunks = new WeakSortedList(32);
        ClearUponReturn = clearUponReturn;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Buffer<T> EmptyBuffer<T>()
        => new(true, 0, [], this, Array.Empty<T>(), 0);


    private int NextAllocSize(int previous,int length, int initial)
    {
        var nextSize = int.Max((length + previous).NextPow2(), previous * 2);
        nextSize = int.Max(initial, nextSize);
        nextSize = int.Min(Array.MaxLength, nextSize);
        return nextSize;
    }
}