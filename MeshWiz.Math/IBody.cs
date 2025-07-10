using System.Numerics;

namespace MeshWiz.Math;

public interface IBody<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    Vector3<TNum> Centroid { get; }
    TNum Volume { get; }
    TNum SurfaceArea { get; }
    IFace<Vector3<TNum>, TNum> Surface { get; }
    BBox3<TNum> BBox { get; }
}