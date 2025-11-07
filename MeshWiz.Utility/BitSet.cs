using System.Collections;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace MeshWiz.Utility;

public sealed class BitSet : IReadOnlyList<bool>
{
    public int Count { get; }
    private int Capacity => _bits.Length * 64;
    private readonly ulong[] _bits;

    public BitSet(int n)
    {
        Count = n;
        var longCount = GetNecessaryLongCount(Count);
        _bits = new ulong[longCount];
    }

    public BitSet(int n, bool initialValue)
    {
        Count = n;
        var longCount = GetNecessaryLongCount(Count);
        _bits = new ulong[longCount];
        if (initialValue) Array.Fill(_bits, ulong.MaxValue);
    }


    /// <inheritdoc />
    public bool this[int index]
    {
        get
        {
            if (Count <= (uint)index)
                return IndexThrowHelper.Throw<bool>(index, Count);
            var (longIndex, shift) = IndexToAdress(index);
            return ((_bits[longIndex] >> shift) & 1) != 0;
        }
        set
        {
            if (Count <= (uint)index)
            {
                IndexThrowHelper.Throw(index, Count);
                return;
            }
            var (word, bit) = IndexToAdress(index);
            var setter = 1UL << bit;
            if (value)
                _bits[word] |= setter; // set bit
            else
                _bits[word] &= ~setter; // clear bit
        }
    }


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetNecessaryLongCount(int bitCount)
        => bitCount % 64 == 0 ? bitCount / 64 : bitCount / 64 + 1;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (int word, int bit) IndexToAdress(int index)
        => (index / 64, index % 64);

    /// <inheritdoc />
    public IEnumerator<bool> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
            yield return this[i];
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}