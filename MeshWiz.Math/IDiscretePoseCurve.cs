using System.Numerics;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public interface IDiscretePoseCurve<TPose, TVec, TNum> : IPoseCurve<TPose,TVec,TNum>,IContiguousDiscreteCurve<TVec, TNum>
    where TPose : IPose<TPose, TVec, TNum>
    where TVec : unmanaged, IVec<TVec, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    TPose StartPose { get; }
    TPose EndPose { get; }


    /// <inheritdoc />
    bool ICurve<TVec, TNum>.IsClosed => TPose.Distance(StartPose,EndPose).IsApproxZero();
    
    PosePolyline<TPose,TVec, TNum> ToPosePolyline();
    PosePolyline<TPose,TVec, TNum> ToPosePolyline(PolylineTessellationParameter<TNum> tessellationParameter);
}