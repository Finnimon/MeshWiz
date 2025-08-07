using System.Numerics;

namespace MeshWiz.Math;

public interface ISurface<out TVector, out TNum> : IShape<TVector>
where TNum: unmanaged, IFloatingPointIeee754<TNum>
where TVector:unmanaged,IFloatingVector<TVector,TNum>
{
    public TNum SurfaceArea { get; }
}