using System.Numerics;

namespace MeshWiz.Math;

public interface IFace<TVector, TNum>
where TNum: unmanaged, IBinaryFloatingPointIeee754<TNum>
where TVector:unmanaged,IFloatingVector<TVector,TNum>
{
    ICurve<TVector, TNum> Bounds { get; }
    TVector Centroid { get; }
    public TNum SurfaceArea { get; }
}