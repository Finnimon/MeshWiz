using System.Numerics;
using MeshWiz.Math;

namespace MeshWiz.Slicer;

public interface ISlicer<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public IReadOnlyList<ICurve<Vector3<TNum>, TNum>> Slice(
        IMesh3<TNum> mesh,
        SlicingDirective<TNum> directive);
}