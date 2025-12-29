using System.Numerics;

namespace MeshWiz.Math;

public interface IPose<TSelf, TVec, TNum>
    : IPosition<TSelf, TVec, TNum>,
        ILerp<TSelf, TNum>,
        ITransform<TVec>,
        IEquatable<TSelf>,
        IEqualityOperators<TSelf,TSelf,bool>
    where TSelf : IPose<TSelf, TVec, TNum>
    where TVec : unmanaged, IVec<TVec, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    TVec Front { get; }
}