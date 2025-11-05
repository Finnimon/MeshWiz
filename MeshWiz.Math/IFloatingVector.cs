using System.Diagnostics.Contracts;
using System.Numerics;
using CommunityToolkit.Diagnostics;
using MeshWiz.Contracts;

namespace MeshWiz.Math;

public interface IFloatingVector<TSelf, TNum> 
    : IVector<TSelf, TNum>, IByteSize, IFloatingPointIeee754<TSelf> 
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
    where TSelf : unmanaged, IFloatingVector<TSelf, TNum>
{
    [Pure]
    static virtual bool IsNan(in TSelf vec)
    {
        for (var i = 0; i < TSelf.Dimensions; i++)
            if (TNum.IsNaN(vec[i]))
                return true;
        return false;
    }
    [Pure]
    public Line<TSelf, TNum> LineTo(TSelf end);
    /// <inheritdoc />
    int IFloatingPoint<TSelf>.GetExponentByteCount()
        => ThrowHelper.ThrowNotSupportedException<int>();

    /// <inheritdoc />
    int IFloatingPoint<TSelf>.GetExponentShortestBitLength()
        => ThrowHelper.ThrowNotSupportedException<int>();

    /// <inheritdoc />
    int IFloatingPoint<TSelf>.GetSignificandBitLength()
        => ThrowHelper.ThrowNotSupportedException<int>();

    /// <inheritdoc />
    int IFloatingPoint<TSelf>.GetSignificandByteCount()
        => ThrowHelper.ThrowNotSupportedException<int>();

    
    /// <inheritdoc />
    bool IFloatingPoint<TSelf>.TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;
        return ThrowHelper.ThrowNotSupportedException<bool>();
    }


    /// <inheritdoc />
    bool IFloatingPoint<TSelf>.TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;
        return ThrowHelper.ThrowNotSupportedException<bool>();
    }

    /// <inheritdoc />
    bool IFloatingPoint<TSelf>.TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;
        return ThrowHelper.ThrowNotSupportedException<bool>();
    }

    /// <inheritdoc />
    bool IFloatingPoint<TSelf>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;
        return ThrowHelper.ThrowNotSupportedException<bool>();
    }
}