using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct BBox3<TNum> : IBody<TNum>, IFace<Vector3<TNum>, TNum>, IEquatable<BBox3<TNum>>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public static readonly BBox3<TNum> NegativeInfinity = new(
        new(TNum.PositiveInfinity, TNum.PositiveInfinity, TNum.PositiveInfinity),
        new(TNum.NegativeInfinity, TNum.NegativeInfinity, TNum.NegativeInfinity));

    public readonly Vector3<TNum> Min, Max;
    public BBox3<TNum> BBox => this;
    public Vector3<TNum> Centroid => (Min + Max) / TNum.CreateTruncating(2);
    public Vector3<TNum> Size => Max - Min;
    private Vector3<TNum> Diagonal => Max - Min;

    public TNum Volume => Diagonal.AlignedCuboidVolume;

    public TNum SurfaceArea => CalculateSurfaceArea();

    public IFace<Vector3<TNum>, TNum> Surface => this;

    public Triangle3<TNum>[] TessellatedSurface => GetFaces();

    public BBox3(Vector3<TNum> min, Vector3<TNum> max)
    {
        Min = min;
        Max = max;
    }

    private TNum CalculateSurfaceArea()
    {
        var d = Diagonal;
        var xy = d.X * d.Y;
        var yz = d.Y * d.Z;
        var zx = d.Z * d.X;
        return TNum.CreateTruncating(2) * (xy + yz + zx);
    }

    private Triangle3<TNum>[] GetFaces()
    {
        var (min, max) = (Min, Max);

        // Corner points
        Vector3<TNum> p000 = min; // (min.X, min.Y, min.Z)
        Vector3<TNum> p001 = new(min.X, min.Y, max.Z); // front bottom-left
        Vector3<TNum> p010 = new(min.X, max.Y, min.Z);
        Vector3<TNum> p011 = new(min.X, max.Y, max.Z); // front top-left
        Vector3<TNum> p100 = new(max.X, min.Y, min.Z);
        Vector3<TNum> p101 = new(max.X, min.Y, max.Z); // front bottom-right
        Vector3<TNum> p110 = new(max.X, max.Y, min.Z);
        Vector3<TNum> p111 = max; // (max.X, max.Y, max.Z)

        return
        [
            new(p010, p110, p000), new(p110, p100, p000),
            new(p111, p011, p001), new(p101, p111, p001),

            new(p001, p011, p000), new(p011, p010, p000),

            new(p110, p111, p100), new(p111, p101, p100),

            new(p100, p101, p000), new(p101, p001, p000),

            new(p011, p111, p010), new(p111, p110, p010),
        ];
    }


    public static BBox3<TNum> Combine(in BBox3<TNum> a, in BBox3<TNum> b)
    {
        var (xMin, yMin, zMin) = a.Min;
        var (xMax, yMax, zMax) = a.Max;
        var (xMinB, yMinB, zMinB) = b.Min;
        var (xMaxB, yMaxB, zMaxB) = b.Max;
        xMin = TNum.Min(xMin, xMinB);
        yMin = TNum.Min(yMin, yMinB);
        zMin = TNum.Min(zMin, zMinB);
        xMax = TNum.Max(xMax, xMaxB);
        yMax = TNum.Max(yMax, yMaxB);
        zMax = TNum.Max(zMax, zMaxB);
        return new BBox3<TNum>(
            new Vector3<TNum>(xMin, yMin, zMin),
            new Vector3<TNum>(xMax, yMax, zMax));
    }

    public static BBox3<TNum> IncludePoint(BBox3<TNum> box, in Vector3<TNum> point)
        => box.CombineWith(point);

    public BBox3<TNum> CombineWith(in Vector3<TNum> p) =>
        new(new Vector3<TNum>(
                TNum.Min(Min.X, p.X),
                TNum.Min(Min.Y, p.Y),
                TNum.Min(Min.Z, p.Z)),
            new Vector3<TNum>(
                TNum.Max(Max.X, p.X),
                TNum.Max(Max.Y, p.Y),
                TNum.Max(Max.Z, p.Z))
        );

    public BBox3<TNum> CombineWith(in BBox3<TNum> o)
        => new(new Vector3<TNum>(
                TNum.Min(Min.X, o.Min.X),
                TNum.Min(Min.Y, o.Min.Y),
                TNum.Min(Min.Z, o.Min.Z)),
            new Vector3<TNum>(
                TNum.Max(Max.X, o.Max.X),
                TNum.Max(Max.Y, o.Max.Y),
                TNum.Max(Max.Z, o.Max.Z))
        );

    public static BBox3<TNum> FromPoint(Vector3<TNum> point) => new(point, point);

    public static bool operator !=(in BBox3<TNum> a, in BBox3<TNum> b) => a.Min != b.Min || a.Max != b.Max;
    public static bool operator ==(in BBox3<TNum> a, in BBox3<TNum> b) => a.Min == b.Min && a.Max == b.Max;

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is not BBox3<TNum> box) return false;
        return this == box;
    }

    public bool Equals(BBox3<TNum> other) => this == other;

    public override int GetHashCode() => HashCode.Combine(Min, Max);
}