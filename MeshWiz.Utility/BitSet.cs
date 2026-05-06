using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace MeshWiz.Utility;

public sealed class BitSet : IReadOnlyList<bool>
{
    int IReadOnlyCollection<bool>.Count => (int)Count;
    public uint Count { get; }


    private static int NIntBits
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => nuint.Size * 8;
    }

    private int Capacity => _bits.Length * NIntBits;
    private readonly nuint[] _bits;

    public BitSet(uint n)
    {
        Count = n;
        var longCount = WordCount(n);
        _bits = new nuint[longCount];
    }

    public BitSet(uint n, bool initialValue)
    {
        Count = n;
        var wordCount = WordCount(n);
        if(!initialValue)
        {
            _bits = new nuint[wordCount];
            return;
        }
        _bits=GC.AllocateUninitializedArray<nuint>(wordCount);
        Array.Fill(_bits, nuint.MaxValue);
    }

    bool IReadOnlyList<bool>.this[int index]
    {
        get => this[(uint)index];   
    }

    /// <inheritdoc />
    public bool this[uint index]
    {
        get
        {
            if (Count <= (uint)index)
                IndexThrowHelper.Throw((int)index,(int) Count);
            var (word, bit) = Address(index);
            return ((_bits[word] >> bit) & One) == One;
        }
        set
        {
            if (Count <= (uint)index)
            {
                IndexThrowHelper.Throw((int)index,(int) Count);
                return;
            }

            var (word, bit) = Address(index);
            var setter = One << bit;
            if (value)
                _bits[word] |= setter; // set bit
            else
                _bits[word] &= ~setter; // clear bit
        }
    }

    private const nuint One = 1;
    private const nuint Zero = 0;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int WordCount(uint bitCount)
        => (int)(bitCount % 64 == 0 ? bitCount / 64 : bitCount / 64 + 1);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (int word, int bit) Address(uint index)
    {
        var bitSize = NIntBits;
        return ((int)(index / bitSize), (int)(index % bitSize));
    }

    /// <inheritdoc />
    public IEnumerator<bool> GetEnumerator()
    {
        for (var i = 0u; i < Count; i++)
            yield return this[i];
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}