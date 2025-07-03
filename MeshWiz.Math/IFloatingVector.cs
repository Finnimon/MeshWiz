using System.Diagnostics.Contracts;
using System.Numerics;

namespace MeshWiz.Math;

public interface IFloatingVector<TSelf, TNum> : IVector<TSelf, TNum>
    where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum>
    where TSelf : IFloatingVector<TSelf, TNum>
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