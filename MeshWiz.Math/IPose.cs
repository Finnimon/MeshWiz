using System.Numerics;

namespace MeshWiz.Math;

public interface IPose<TSelf, TVector, TNum>
    : IPosition<TSelf, TVector, TNum>,
        ILerp<TSelf, TNum>,
        ITransform<TVector>,
        IEquatable<TSelf>,
        IEqualityOperators<TSelf,TSelf,bool>
    where TSelf : IPose<TSelf, TVector, TNum>
    where TVector : unmanaged, IVector<TVector, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    TVector Front { get; }
}