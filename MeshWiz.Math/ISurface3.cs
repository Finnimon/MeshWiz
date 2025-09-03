using System.Numerics;

namespace MeshWiz.Math;

public interface ISurface3<TNum> : ISurface<Vector3<TNum>, TNum> , IBounded<Vector3<TNum>>
    where TNum: unmanaged, IFloatingPointIeee754<TNum>
{
    IMesh<TNum> Tessellate();
}