using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct BvhHitInfo<TNum>(TNum distance, int triangleIndex)
{
    public readonly TNum Distance = distance;
    public readonly int TriangleIndex = triangleIndex;
}

