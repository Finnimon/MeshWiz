using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Plane3<TNum>
    : IIntersecter<Line<Vector3<TNum>, TNum>, Vector3<TNum>>,
        IIntersecter<Ray3<TNum>, Vector3<TNum>>,
        IIntersecter<Triangle3<TNum>, Line<Vector3<TNum>, TNum>>,
        IIntersecter<BBox3<TNum>, Quad3<TNum>>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public static Plane3<TNum> XY => new(Vector3<TNum>.UnitZ, TNum.Zero);
    public static Plane3<TNum> YZ => new(Vector3<TNum>.UnitX, TNum.Zero);
    public static Plane3<TNum> ZX => new(Vector3<TNum>.UnitY, TNum.Zero);
    
    public readonly TNum D;
    public readonly Vector3<TNum> Normal;
    public Vector4<TNum> AsVector4 => new(Normal, D);

    public Plane3(in Triangle3<TNum> triangleOnPlane) : this(triangleOnPlane.Normal, triangleOnPlane.A) { }

    public Plane3(Vector3<TNum> normal, Vector3<TNum> pointOnPlane)
    {
        Normal = normal;
        D = -Normal * pointOnPlane;
    }
    public Plane3(Vector3<TNum> a, Vector3<TNum> b, Vector3<TNum> c)
    {
        Normal = (a - b) ^ (c - a);
        Normal = Normal.Normalized;
        D = -Normal * a;
    }

    public Plane3(Vector4<TNum> asVec4) : this(asVec4.XYZ, asVec4.W) { }

    public Plane3(Vector3<TNum> normal, TNum d)
    {
        Normal = normal.Normalized;
        D = d;
    }


    public bool DoIntersect(Line<Vector3<TNum>, TNum> test)
        => TNum.Sign(SignedDistance(test.Start))
           != TNum.Sign(SignedDistance(test.End));

    public bool Intersect(Line<Vector3<TNum>, TNum> test, out Vector3<TNum> result)
    {
        var denominator = Normal * test.NormalDirection;
        // Check if ray is parallel to the plane
        if (denominator.IsApprox(TNum.Zero))
        {
            result = Vector3<TNum>.NaN;
            return false;
        }

        // Compute intersection distance along ray direction
        var t = -(Normal * test.Start + D) / denominator;
        result = test.TraverseOnCurve(t);
        return TNum.NegativeZero <= t && t <= TNum.One;
    }

    private Vector3<TNum> ForceIntersect(Line<Vector3<TNum>, TNum> line)
    {
        var denominator = Normal * line.Direction;
        var t = -(Normal * line.Start + D) / denominator;
        return line.Traverse(t);
    }

    public bool DoIntersect(Ray3<TNum> test)
        => TNum.Abs(test.Direction * Normal) < TNum.Epsilon;

    public bool Intersect(Ray3<TNum> test, out Vector3<TNum> result)
    {
        var denominator = Normal * test.Direction;
        // Check if ray is parallel to the plane
        if (TNum.Abs(denominator) < TNum.Epsilon)
        {
            result = Vector3<TNum>.NaN;
            return false;
        }

        // Compute intersection distance along ray direction
        var t = -(Normal * test.Origin + D) / denominator;
        result = test * t;
        return t >= TNum.Zero;
    }

    public bool DoIntersect(Triangle3<TNum> test)
    {
        var a = DistanceSign(test.A);
        var b = DistanceSign(test.B);
        var c = DistanceSign(test.C);
        return a != b || b != c;
    }

    public bool Intersect(Triangle3<TNum> test, out Line<Vector3<TNum>, TNum> result)
    {
        var (a, b, c) = test;
        var aSign = DistanceSign(a);
        var bSign = DistanceSign(b);
        var cSign = DistanceSign(c);
        var aUnique = aSign != bSign && aSign != cSign;
        if (aUnique)
        {
            result = ComputeTriangleIntersection(a, b, c,aSign);
            return true;
        }

        var bUnique = bSign != cSign && bSign != aSign;
        if (bUnique)
        {
            result = ComputeTriangleIntersection(b, c, a,bSign);
            return true;
        }

        var cUnique = cSign != aSign && cSign != bSign;
        if (cUnique)
        {
            result = ComputeTriangleIntersection(c, a, b,cSign);
            return true;
        }

        result = default;
        return false;
    }

    private Line<Vector3<TNum>, TNum> ComputeTriangleIntersection(Vector3<TNum> aUnique, Vector3<TNum> b,
        Vector3<TNum> c, int aDistanceSign)
    {
        var ab = aUnique.LineTo(b);
        var ac = aUnique.LineTo(c);
        var start = ForceIntersect(ab);
        var end = ForceIntersect(ac);
        return aDistanceSign>0 ? start.LineTo(end):end.LineTo(start);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum SignedDistance(Vector3<TNum> p) => Normal * p + D;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int DistanceSign(Vector3<TNum> p) => TNum.Sign(SignedDistance(p));

    public bool DoIntersect(BBox3<TNum> box)
    {
        var boxMin = box.Min;
        var boxMax = box.Max;
        var d0 = DistanceSign(boxMin);
        var d1 = DistanceSign(new(boxMax.X, boxMin.Y, boxMin.Z));
        var d2 = DistanceSign(new(boxMax.X, boxMax.Y, boxMin.Z));
        var d3 = DistanceSign(new(boxMin.X, boxMax.Y, boxMin.Z));
        var d4 = DistanceSign(new(boxMin.X, boxMin.Y, boxMax.Z));
        var d5 = DistanceSign(new(boxMax.X, boxMin.Y, boxMax.Z));
        var d6 = DistanceSign(boxMax);
        var d7 = DistanceSign(new(boxMin.X, boxMax.Y, boxMax.Z));

        // Track whether we saw both sides of the plane
        return d0 != d1 || d1 != d2 || d2 != d3 || d3 != d4 || d4 != d5 || d5 != d6 || d6 != d7;
    }


    public bool Intersect(BBox3<TNum> box, out Quad3<TNum> result)
    {
        // 1) Build the 8 corners
        var min = box.Min;
        var max = box.Max;
        var v0 = new Vector3<TNum>(min.X, min.Y, min.Z);
        var v1 = new Vector3<TNum>(max.X, min.Y, min.Z);
        var v2 = new Vector3<TNum>(max.X, max.Y, min.Z);
        var v3 = new Vector3<TNum>(min.X, max.Y, min.Z);
        var v4 = new Vector3<TNum>(min.X, min.Y, max.Z);
        var v5 = new Vector3<TNum>(max.X, min.Y, max.Z);
        var v6 = new Vector3<TNum>(max.X, max.Y, max.Z);
        var v7 = new Vector3<TNum>(min.X, max.Y, max.Z);

        // 2) The 12 edges as Line segments
        var edges = new[]
        {
            v0.LineTo(v1), v1.LineTo(v2), v2.LineTo(v3), v3.LineTo(v0), // bottom
            v4.LineTo(v5), v5.LineTo(v6), v6.LineTo(v7), v7.LineTo(v4), // top
            v0.LineTo(v4), v1.LineTo(v5), v2.LineTo(v6), v3.LineTo(v7), // verticals
        };

        // 3) Clip each against the plane, collect unique hits
        var pts = new List<Vector3<TNum>>();
        foreach (var edge in edges)
        {
            if (!Intersect(edge, out var p)) continue;
            bool duplicate = false;
            foreach (var q in pts)
            {
                if ((p - q).SquaredLength > TNum.Epsilon) continue;
                duplicate = true;
                break;
            }

            if (!duplicate) pts.Add(p);
        }

        // 4) Need at least a triangle
        if (pts.Count < 3)
        {
            result = default;
            return false;
        }

        // 5) Choose projection plane by dropping the largest normal component
        var an = new[] { TNum.Abs(Normal.X), TNum.Abs(Normal.Y), TNum.Abs(Normal.Z) };
        int dropAxis = an[0] >= an[1] && an[0] >= an[2] ? 0
            : an[1] >= an[2] ? 1
            : 2;

        // 6) Compute centroid
        var c = new Vector3<TNum>(
            pts.Aggregate(TNum.Zero, (s, v) => s + v.X) / TNum.CreateTruncating(pts.Count),
            pts.Aggregate(TNum.Zero, (s, v) => s + v.Y) / TNum.CreateTruncating(pts.Count),
            pts.Aggregate(TNum.Zero, (s, v) => s + v.Z) / TNum.CreateTruncating(pts.Count)
        );

        // 7) Sort CCW around centroid in the chosen 2D projection
        var ordered = pts
            .Select(p =>
            {
                // pick 2D coords
                var (ux, uy, vx, vy) =
                    dropAxis switch
                    {
                        0 => (p.Y, p.Z, c.Y, c.Z),
                        1 => (p.X, p.Z, c.X, c.Z),
                        _ => (p.X, p.Y, c.X, c.Y)
                    };
                var angle = TNum.Atan2(uy - vy, ux - vx);

                return (point: p, angle);
            })
            .OrderBy(pa => pa.angle)
            .Select(pa => pa.point)
            .ToList();

        // 8) Build Quad3 (or degenerate)
        result = ordered.Count switch
        {
            3 => new Quad3<TNum>(ordered[0], ordered[1], ordered[2], ordered[2]),
            _ => new Quad3<TNum>(ordered[0], ordered[1], ordered[2], ordered[3])
        };

        return true;
    }
}