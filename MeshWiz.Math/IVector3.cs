using System.Diagnostics.Contracts;
using System.Numerics;

namespace MeshWiz.Math;

public interface IVector3<TSelf, TNum> : IFloatingVector<TSelf, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
    where TSelf : unmanaged, IVector3<TSelf, TNum>
{
    [Pure]static uint IVector<TSelf, TNum>.Dimensions => 3;
    [Pure]int IReadOnlyCollection<TNum>.Count => 3;
    
    [Pure]TNum AlignedCuboidVolume => TNum.Abs(this[0] * this[1] * this[2]);
    [Pure]TSelf Cross(TSelf other);
    [Pure]
    static virtual TSelf operator ^(TSelf left, TSelf right) => left.Cross(right);
    [Pure] static abstract TSelf FromXYZ(TNum x,TNum y ,TNum z);
    [Pure] TSelf ZYX=>TSelf.FromXYZ(this[2],this[1],this[0]);
    [Pure] TSelf YZX=>TSelf.FromXYZ(this[1],this[2],this[0]);
    [Pure] TSelf YXZ=>TSelf.FromXYZ(this[1],this[0],this[2]);
    [Pure] TSelf XZY=>TSelf.FromXYZ(this[0],this[2],this[1]);
    [Pure] TSelf ZXY=>TSelf.FromXYZ(this[2],this[0],this[1]);
    [Pure] static TSelf IFloatingVector<TSelf,TNum>.NaN=>TSelf.FromXYZ(TNum.NaN,TNum.NaN,TNum.NaN);
    [Pure] static TSelf IVector<TSelf,TNum>.One=>TSelf.FromXYZ(TNum.One,TNum.One,TNum.One);
}