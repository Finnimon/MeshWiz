using System.Numerics;

namespace MeshWiz.Math;

public interface IBody<TNum>
    where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum>
{
    Vector3<TNum> Centroid { get; }
    TNum Volume { get; }
    TNum SurfaceArea { get; }
    IFace<Vector3<TNum>, TNum>[] Surface { get; }
    Triangle3<TNum>[] TessellatedSurface { get; }
    BBox3<TNum> BBox { get; }
}