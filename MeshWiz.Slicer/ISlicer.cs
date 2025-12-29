using System.Numerics;
using MeshWiz.Math;

namespace MeshWiz.Slicer;

public interface ISlicer<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public IReadOnlyList<ICurve<Vec3<TNum>, TNum>> Slice(
        IMesh<TNum> mesh,
        SlicingDirective<TNum> directive);
}