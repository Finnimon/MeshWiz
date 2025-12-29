using System.Numerics;
using MeshWiz.Math;

namespace MeshWiz.Slicer;

public interface IPreProcessor<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public IReadOnlyList<ToolCenterPoint<TNum>> CreateToolpath(
        IReadOnlyList<ICurve<Vec3<TNum>, TNum>> printingLines,
        SlicingDirective<TNum> directive);
}