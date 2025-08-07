using System.Numerics;

namespace MeshWiz.Math;

public interface ISurface3<TNum> : ISurface<Vector3<TNum>, TNum> , IBounded<BBox3<TNum>>
    where TNum: unmanaged, IFloatingPointIeee754<TNum>
{
    IMesh3<TNum> Tessellate();
}