using System.Numerics;

namespace MeshWiz.Math;

public interface IPose<TSelf, TVector, TNum>
    : IPosition<TSelf, TVector, TNum>,
        ILerp<TSelf, TNum>,
        ITransform<TVector>
    where TSelf : IPosition<TSelf, TVector, TNum>, ILerp<TSelf, TNum>
    where TVector : unmanaged, IVector<TVector, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    TVector Front { get; }
}