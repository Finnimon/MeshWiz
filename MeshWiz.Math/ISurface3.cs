using System.Numerics;

namespace MeshWiz.Math;

public interface ISurface3<TNum> : ISurface<Vec3<TNum>, TNum> , IBounded<Vec3<TNum>>
    where TNum: unmanaged, IFloatingPointIeee754<TNum>
{
    IMesh<TNum> Tessellate();
}