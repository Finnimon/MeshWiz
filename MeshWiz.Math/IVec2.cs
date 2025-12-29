using System.Diagnostics.Contracts;
using System.Numerics;

namespace MeshWiz.Math;

public interface IVec2<TSelf, TNum> : IVec<TSelf, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
    where TSelf : unmanaged, IVec2<TSelf, TNum>
{
    [Pure]static int IVecBase<TSelf, TNum>.Dimensions => 2;
    [Pure]int IReadOnlyCollection<TNum>.Count => 2;
    [Pure]TNum Cross(TSelf r);
    [Pure]int CrossSign(TSelf other)=>TNum.Sign(Cross(other));
    [Pure]TNum AlignedSquareVolume=>this[0] * this[1];
    static abstract TSelf FromXY(TNum x, TNum y);
    [Pure] TSelf YX => TSelf.FromXY(this[1], this[0]);
    
}