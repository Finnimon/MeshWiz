using System.Numerics;

namespace MeshWiz.Slicer;

public interface IPostProcessor<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public void ExportToolpath(Stream stream, IReadOnlyList<ToolCenterPoint<TNum>> printingLines,
        SlicingDirective<TNum> directive);
}