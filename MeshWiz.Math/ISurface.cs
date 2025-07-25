using System.Numerics;

namespace MeshWiz.Math;

public interface ISurface<out TVector, out TNum> : IShape<TVector>
where TNum: unmanaged, IFloatingPointIeee754<TNum>
where TVector:unmanaged,IFloatingVector<TVector,TNum>
{
    public TNum SurfaceArea { get; }
}

public interface ISurface3<TNum> : ISurface<Vector3<TNum>, TNum> , IBounded<BBox3<TNum>>
where TNum: unmanaged, IFloatingPointIeee754<TNum>
{
    IMesh3<TNum> Tessellate();
}

public interface IBounded<TBBox>
{
    TBBox BBox { get; }
}