using System.Numerics;

namespace MeshWiz.Math;

public interface ISurface<out TVec, out TNum> : IShape<TVec>
where TNum: unmanaged, IFloatingPointIeee754<TNum>
where TVec:unmanaged,IVec<TVec,TNum>
{
    public TNum SurfaceArea { get; }
}