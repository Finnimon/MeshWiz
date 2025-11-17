using System.Numerics;

namespace MeshWiz.Math;

public interface IDiscreteCurve<TVector, TNum> : ICurve<TVector, TNum>
    where TVector : unmanaged, IVector<TVector, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    TVector Start { get; }
    TVector End { get; }
    TVector TraverseOnCurve(TNum distance);
    TNum Length { get; }
    bool ICurve<TVector, TNum>.IsClosed=>Start.Equals(End);
    
    
    Polyline<TVector, TNum> ToPolyline();
    Polyline<TVector, TNum> ToPolyline(PolylineTessellationParameter<TNum> tessellationParameter);
}