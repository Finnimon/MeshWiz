using System.Diagnostics.Contracts;
using System.Numerics;
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

    public Line<TSelf, TNum> LineTo(TSelf end);
    /// <inheritdoc />
    int IFloatingPoint<TSelf>.GetExponentByteCount()
        => throw new NotSupportedException();

    /// <inheritdoc />
    int IFloatingPoint<TSelf>.GetExponentShortestBitLength()
        => throw new NotSupportedException();

    /// <inheritdoc />
    int IFloatingPoint<TSelf>.GetSignificandBitLength()
        => throw new NotSupportedException();

    /// <inheritdoc />
    int IFloatingPoint<TSelf>.GetSignificandByteCount()
        => throw new NotSupportedException();

    
    /// <inheritdoc />
    bool IFloatingPoint<TSelf>.TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten)
        => throw new NotSupportedException();


    /// <inheritdoc />
    bool IFloatingPoint<TSelf>.TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten)
        => throw new NotSupportedException();

    /// <inheritdoc />
    bool IFloatingPoint<TSelf>.TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten)
        => throw new NotSupportedException();

    /// <inheritdoc />
    bool IFloatingPoint<TSelf>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
        => throw new NotSupportedException();

}