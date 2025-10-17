using System.Numerics;

namespace MeshWiz.Math;

public interface IContiguousCurve<TVector, TNum> : ICurve<TVector,TNum> 
    where TNum : unmanaged, IFloatingPointIeee754<TNum> 
    where TVector : unmanaged, IFloatingVector<TVector, TNum>
{
    public TVector GetTangent(TNum at);
}