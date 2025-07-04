using System.Numerics;
using System.Runtime.InteropServices;
namespace MeshWiz.Math;
[StructLayout(LayoutKind.Sequential)]
public readonly struct BBox3<TNum> : IBody<TNum>
    where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum>
{
    public static BBox3<TNum> NegativeInfinity=>new(
        new(TNum.PositiveInfinity, TNum.PositiveInfinity, TNum.PositiveInfinity),
        new(TNum.NegativeInfinity, TNum.NegativeInfinity, TNum.NegativeInfinity));

    public readonly Vector3<TNum> Min, Max;
    public BBox3<TNum> BBox => this;
    public Vector3<TNum> Centroid => (Min + Max) / TNum.CreateTruncating(2);

    private Vector3<TNum> Diagonal => Max - Min;

    public TNum Volume => Diagonal.AlignedCuboidVolume;

    public TNum SurfaceArea => CalculateSurfaceArea();

    public IFace<Vector3<TNum>, TNum>[] Surface => [..TessellatedSurface];

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
        Vector3<TNum> p000 = min;
        Vector3<TNum> p001 = new (min.X, min.Y, max.Z);
        Vector3<TNum> p010 = new (min.X, max.Y, min.Z);
        Vector3<TNum> p011 = new (min.X, max.Y, max.Z);
        Vector3<TNum> p100 = new (max.X, min.Y, min.Z);
        Vector3<TNum> p101 = new (max.X, min.Y, max.Z);
        Vector3<TNum> p110 = new (max.X, max.Y, min.Z);
        Vector3<TNum> p111 = max;

        return
        [
            new(p000, p010, p110), new(p000, p110, p100),
            new(p001, p101, p111), new(p001, p111, p011),
            new(p000, p001, p011), new(p000, p011, p010),
            new(p100, p110, p111), new(p100, p111, p101),
            new(p000, p100, p101), new(p000, p101, p001),
            new(p010, p011, p111), new(p010, p111, p110),
        ];
    }

    public bool Intersect(ICurve<Vector3<TNum>, TNum> curve) => throw new NotImplementedException();
    public bool Contains(Vector3<TNum> point) => throw new NotImplementedException();
    public bool Intersect(IBody<TNum> body) => throw new NotImplementedException();

    public static BBox3<TNum> Combine(in BBox3<TNum> a,in BBox3<TNum> b)
    {
        var (xMin, yMin, zMin) = a.Min;
        var (xMax, yMax, zMax) = a.Max;
        var (xMinB, yMinB, zMinB) = b.Min;
        var (xMaxB,yMaxB,zMaxB) = b.Max;
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
    public static BBox3<TNum> IncludePoint(in BBox3<TNum> box,in Vector3<TNum> point)
    {
        var (xMin, yMin, zMin) = box.Min;
        var (xMax, yMax, zMax) = box.Max;
        var (xPoint, yPoint, zPoint) = point;
        xMin = TNum.Min(xMin, xPoint);
        yMin = TNum.Min(yMin, yPoint);
        zMin = TNum.Min(zMin, zPoint);
        xMax = TNum.Max(xMax, xPoint);
        yMax = TNum.Max(yMax, yPoint);
        zMax = TNum.Max(zMax, zPoint);
        return new BBox3<TNum>(
            new Vector3<TNum>(xMin, yMin, zMin),
            new Vector3<TNum>(xMax, yMax, zMax));
    }

    public BBox3<TNum> CombineWith(in Vector3<TNum> point)
        => BBox3<TNum>.IncludePoint(in this, in point);

    public BBox3<TNum> CombineWith(in BBox3<TNum> other)
        => BBox3<TNum>.Combine(in this, in other);




}
