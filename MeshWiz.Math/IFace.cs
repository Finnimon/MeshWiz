using System.Numerics;

namespace MeshWiz.Math;

public interface IFace<TVector, TNum>
where TNum: unmanaged, IFloatingPointIeee754<TNum>
where TVector:unmanaged,IFloatingVector<TVector,TNum>
{
    TVector Centroid { get; }
    public TNum SurfaceArea { get; }
}