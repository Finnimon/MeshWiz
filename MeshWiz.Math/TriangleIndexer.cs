using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct TriangleIndexer(uint a, uint b, uint c)
{
    public readonly uint A = a, B = b, C = c;
    public Triangle3<TNum> Extract<TNum>(Vector3<TNum>[] vertices)
        where TNum : unmanaged, IFloatingPointIeee754<TNum> 
        => new(vertices[A], vertices[B], vertices[C]);

    public void Deconstruct(out uint a, out uint b, out uint c)
    {
        a = A;
        b = B;
        c = C;
    }
}