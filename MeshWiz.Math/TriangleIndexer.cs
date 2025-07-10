using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct TriangleIndexer(int a, int b, int c)
{
    public readonly int A = a, B = b, C = c;
    public Triangle3<TNum> Extract<TNum>(Vector3<TNum>[] vertices)
        where TNum : unmanaged, IFloatingPointIeee754<TNum> 
        => new(vertices[A], vertices[B], vertices[C]);
}