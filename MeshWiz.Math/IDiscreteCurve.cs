using System.Numerics;

namespace MeshWiz.Math;

public interface IDiscreteCurve<TVector, TNum> : ICurve<TVector, TNum>
    where TVector : unmanaged, IFloatingVector<TVector, TNum>
    where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum>
{
    TVector Start { get; }
    TVector End { get; }
    TVector TraverseOnCurve(TNum distance);
    TNum Length { get; }
    bool ICurve<TVector, TNum>.IsClosed=>Start.Equals(End);
}