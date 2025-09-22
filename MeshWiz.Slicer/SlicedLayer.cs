using System.Numerics;

namespace MeshWiz.Slicer;

public record SlicedLayer<TNum>(
    IReadOnlyList<ToolCenterPoint<TNum>[]> Perimeter,
    IReadOnlyList<ToolCenterPoint<TNum>[]> Infill,
    TNum Layer)
    where TNum : unmanaged, IFloatingPointIeee754<TNum>;