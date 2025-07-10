using System.Diagnostics.Contracts;
using System.Numerics;

namespace MeshWiz.Math;

public interface IVector2<TSelf, TNum> : IFloatingVector<TSelf, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
    where TSelf : unmanaged, IVector2<TSelf, TNum>
{
    [Pure]static uint IVector<TSelf, TNum>.Dimensions => 2;
    [Pure]int IReadOnlyCollection<TNum>.Count => 2;
    [Pure]TNum Cross(in TSelf other);
    [Pure]int CrossSign(in TSelf other)=>TNum.Sign(Cross(in other));
    [Pure]TNum AlignedSquareVolume=>this[0] * this[1];
    static abstract TSelf FromXY(TNum x, TNum y);
    [Pure] TSelf YX => TSelf.FromXY(this[1], this[0]);
    
    [Pure] static TSelf IFloatingVector<TSelf,TNum>.NaN=>TSelf.FromXY(TNum.NaN,TNum.NaN);
    [Pure] static TSelf IVector<TSelf,TNum>.One=>TSelf.FromXY(TNum.One,TNum.One);
}