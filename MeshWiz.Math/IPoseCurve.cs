using System.Numerics;

namespace MeshWiz.Math;

public interface IPoseCurve<out TPose, TVec, TNum> : IContiguousCurve<TVec, TNum>
    where TPose : IPose<TPose, TVec, TNum>
    where TVec : unmanaged, IVec<TVec, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum> 
{
    TPose GetPose(TNum t);
}