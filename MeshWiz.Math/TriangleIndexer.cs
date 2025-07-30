using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct TriangleIndexer(int a, int b, int c) : IEquatable<TriangleIndexer>
{
    public readonly int A = a, B = b, C = c;

    public Triangle3<TNum> Extract<TNum>(IReadOnlyList<Vector3<TNum>> vertices)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        => new(vertices[A], vertices[B], vertices[C]);

    public Triangle<TVector, TNum> Extract<TVector, TNum>(IReadOnlyList<TVector> vertices)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        where TVector : unmanaged, IFloatingVector<TVector, TNum>
        => new(vertices[A], vertices[B], vertices[C]);

    public void Deconstruct(out int a, out int b, out int c)
    {
        a = A;
        b = B;
        c = C;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(A, B, C);
    }

    public override bool Equals(object? obj) => obj is TriangleIndexer other && Equals(other);

    public bool Equals(TriangleIndexer other) => A == other.A && B == other.B && C == other.C;

    public static bool operator ==(TriangleIndexer left, TriangleIndexer right) => left.Equals(right);

    public static bool operator !=(TriangleIndexer left, TriangleIndexer right) => !(left.Equals(right));
}