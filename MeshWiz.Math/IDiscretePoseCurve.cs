using System.Numerics;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public interface IDiscretePoseCurve<TPose, TVector, TNum> : IPoseCurve<TPose,TVector,TNum>,IContiguousDiscreteCurve<TVector, TNum>
    where TPose : IPose<TPose, TVector, TNum>
    where TVector : unmanaged, IVector<TVector, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    TPose StartPose { get; }
    TPose EndPose { get; }


    /// <inheritdoc />
    bool ICurve<TVector, TNum>.IsClosed => TPose.Distance(StartPose,EndPose).IsApproxZero();
    
    PosePolyline<TPose,TVector, TNum> ToPosePolyline();
    PosePolyline<TPose,TVector, TNum> ToPosePolyline(PolylineTessellationParameter<TNum> tessellationParameter);
}