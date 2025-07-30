using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct LineIndexer(int start, int end) : IEquatable<LineIndexer>
{
    public readonly int Start=start,  End=end;

    public Line<TVector, TNum> Extract<TVector, TNum>(IReadOnlyList<TVector> vertices)
        where TVector : unmanaged, IFloatingVector<TVector, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        => vertices[Start].LineTo(vertices[End]);
    
    
    public bool Equals(LineIndexer other) => Start == other.Start && End == other.End;

    public override bool Equals(object? obj) => obj is LineIndexer other && Equals(other);
    public static bool operator ==(LineIndexer left, LineIndexer right) => left.Equals(right);
    public static bool operator !=(LineIndexer left, LineIndexer right) => !left.Equals(right);

    public override int GetHashCode() => HashCode.Combine(Start, End);
}