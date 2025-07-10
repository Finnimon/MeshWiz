using System.Numerics;
using MeshWiz.Math;

namespace MeshWiz.Slicer;

public readonly struct ToolCenterPoint<TNum> where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum>
{
    public readonly Vector3<TNum> Position;
}