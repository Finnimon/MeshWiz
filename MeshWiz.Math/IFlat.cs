using System.Numerics;

namespace MeshWiz.Math;

public interface IFlat<TNum>
where TNum: unmanaged, IFloatingPointIeee754<TNum>

{
    public Vector3<TNum> Normal { get; }
    public Plane3<TNum> ToPlane { get; }
}