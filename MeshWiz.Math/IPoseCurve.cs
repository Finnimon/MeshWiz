using System.Numerics;

namespace MeshWiz.Math;

public interface IPoseCurve<out TPose, TVector, TNum> : IContiguousCurve<TVector, TNum>
    where TPose : IPose<TPose, TVector, TNum>
    where TVector : unmanaged, IVector<TVector, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum> 
{
    TPose GetPose(TNum t);
}