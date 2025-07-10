using System.Diagnostics.Contracts;
using System.Numerics;
using MeshWiz.Contracts;

namespace MeshWiz.Math;

public interface IFloatingVector<TSelf, TNum> : IVector<TSelf, TNum>, IByteSize
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
    where TSelf : unmanaged, IFloatingVector<TSelf, TNum>
{
    [Pure]
    static abstract TSelf NaN { get; }
    [Pure]
    static virtual bool IsNan(in TSelf vec)
    {
        for (var i = 0; i < TSelf.Dimensions; i++)
            if (!TNum.IsNaN(vec[i]))
                return false;
        return true;
    }
}