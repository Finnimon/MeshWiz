using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct TriangleIndexer(uint a, uint b, uint c) : IEquatable<TriangleIndexer>
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

    public override int GetHashCode()
    {
        return HashCode.Combine(A, B, C);
    }

    public override bool Equals(object? obj)=>obj is TriangleIndexer other&&  Equals(other);

    public bool Equals(TriangleIndexer other) => A == other.A && B == other.B && C == other.C;
}