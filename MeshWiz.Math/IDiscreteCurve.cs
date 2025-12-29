using System.Numerics;

namespace MeshWiz.Math;

public interface IDiscreteCurve<TVec, TNum> : ICurve<TVec, TNum>
    where TVec : unmanaged, IVec<TVec, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    TVec Start { get; }
    TVec End { get; }
    TVec TraverseOnCurve(TNum t);
    TNum Length { get; }
    bool ICurve<TVec, TNum>.IsClosed=>Start.Equals(End);
    
    
    Polyline<TVec, TNum> ToPolyline();
    Polyline<TVec, TNum> ToPolyline(PolylineTessellationParameter<TNum> tessellationParameter);
}