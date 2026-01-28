using System.Diagnostics.Contracts;
using System.Numerics;

namespace MeshWiz.Math;

public interface IVec2<TSelf, TNum> : IVec<TSelf, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
    where TSelf : unmanaged, IVec2<TSelf, TNum>
{
    [Pure]static int IVecBase<TSelf, TNum>.Dimensions => 2;
    [Pure]TNum Cross(TSelf r);
    [Pure]int CrossSign(TSelf other)=>TNum.Sign(Cross(other));
    [Pure]TNum AlignedSquareVolume=>this[0] * this[1];
    [Pure] TSelf YX { get; }

}