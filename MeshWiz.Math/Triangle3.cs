using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MeshWiz.Contracts;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Triangle3<TNum> : ISurface<Vec3<TNum>, TNum>, IFlat<TNum>, IByteSize, IBounded<Vec3<TNum>>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Vec3<TNum> A, B, C;
    public Line<Vec3<TNum>, TNum> Ab => A.LineTo(B);
    public Line<Vec3<TNum>, TNum> Bc => B.LineTo(C);
    public Line<Vec3<TNum>, TNum> Ca => A.LineTo(B);
    public Vec3<TNum> Normal => ((B - A).Cross(C - A)).Normalized();
    public Plane<TNum> Plane => new(in this);

    public Triangle3(Vec3<TNum> a, Vec3<TNum> b, Vec3<TNum> c)
    {
        A = a;
        B = b;
        C = c;
    }

    public ICurve<Vec3<TNum>, TNum> Bounds => new Polyline<Vec3<TNum>, TNum>([A, B, C]);


    public Vec3<TNum> Centroid => (A + B + C) / TNum.CreateTruncating(3);

    public TNum SurfaceArea
    {
        get
        {
            var ab = B - A;
            var ac = C - A;
            var abAcDot = ab.Dot(ac);
            return TNum.Sqrt((ab.Dot(ab)) * (ac.Dot(ac)) - abAcDot * abAcDot) / TNum.CreateTruncating(2);
        }
    }

    public static implicit operator Triangle3<TNum>(Triangle<Vec3<TNum>, TNum> dimensionless)
        => new(dimensionless.A, dimensionless.B, dimensionless.C);

    public static int ByteSize => Vec3<TNum>.ByteSize * 3;

    public void Deconstruct(out Vec3<TNum> a, out Vec3<TNum> b, out Vec3<TNum> c)
    {
        a = A;
        b = B;
        c = C;
    }

    public (TNum dAB, TNum dBC, TNum dCA) EdgeLengths()
    {
        var ab = B.Subtract(A).Length;
        var bc = C.Subtract(B).Length;
        var ca = A.Subtract(B).Length;
        return (ab, bc, ca);
    }

    public AABB<Vec3<TNum>> BBox => AABB<Vec3<TNum>>.From(A, B, C);

    [System.Diagnostics.Contracts.Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Triangle3<TOtherNum> To<TOtherNum>()
        where TOtherNum : unmanaged, IFloatingPointIeee754<TOtherNum> =>
        new(A.To<TOtherNum>(), B.To<TOtherNum>(), C.To<TOtherNum>());

    public static Triangle3<TNum> Transform(Mat4x4<TNum> m, Triangle3<TNum> t)
        => new(Mat4x4<TNum>.MultiplyPoint(m, t.A), Mat4x4<TNum>.MultiplyPoint(m, t.B),
            Mat4x4<TNum>.MultiplyPoint(m, t.C));

    public static bool DoIntersect(Triangle3<TNum> a, Triangle3<TNum> b)
    {
        var pA = a.Plane;
        if (!pA.DoIntersect(b))
            return false;
        
        var pB=b.Plane;
        if (!pB.DoIntersect(a))
            return false;

        var dir = pA.Normal.Cross(pB.Normal);
        var ray= a.A.RayThrough(dir);
        var intervalA = ProjectOnto(a, ray);
        var intervalB = ProjectOnto(b, ray);
        return intervalA.IntersectsWith(intervalB);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AABB<TNum> ProjectOnto(Triangle3<TNum> tri, Ray3<TNum> line) =>
        AABB.From(line.ParameterOfClosestPoint(tri.A),
            line.ParameterOfClosestPoint(tri.B),
            line.ParameterOfClosestPoint(tri.C));

}