using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using MeshWiz.Contracts;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Triangle3<TNum> : ISurface<Vec3<TNum>, TNum>, IFlat<TNum>, IByteSize, IBounded<Vec3<TNum>>, IEquatable<Triangle3<TNum>>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    [JsonInclude]
    public readonly Vec3<TNum> A, B, C;
    [JsonIgnore] public Line<Vec3<TNum>, TNum> Ab => A.LineTo(B);
    [JsonIgnore] public Line<Vec3<TNum>, TNum> Bc => B.LineTo(C);
    [JsonIgnore] public Line<Vec3<TNum>, TNum> Ca => A.LineTo(B);
    [JsonIgnore] public Vec3<TNum> Normal => ((B - A).Cross(C - A)).Normalized();
    [JsonIgnore] public Plane<TNum> Plane => new(in this);

    [JsonConstructor]
    public Triangle3(Vec3<TNum> a, Vec3<TNum> b, Vec3<TNum> c)
    {
        A = a;
        B = b;
        C = c;
    }

    [JsonIgnore] public ICurve<Vec3<TNum>, TNum> Bounds => new Polyline<Vec3<TNum>, TNum>([A, B, C, A]);


    [JsonIgnore] public Vec3<TNum> Centroid => (A + B + C) / TNum.CreateTruncating(3);

    [JsonIgnore]
    public TNum SurfaceArea
    {
        get
        {
            var ab = B - A;
            var ac = C - A;
            var abAcDot = Vec3<TNum>.Dot(ab,ac);
            return TNum.Sqrt((ab.Dot(ab)) * (ac.Dot(ac)) - abAcDot * abAcDot) * Numbers<TNum>.Half;
        }
    }

    public static implicit operator Triangle3<TNum>(Triangle<Vec3<TNum>, TNum> dimensionless)
        => new(dimensionless.A, dimensionless.B, dimensionless.C);

    [JsonIgnore] public static int ByteSize => Vec3<TNum>.ByteSize * 3;

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

    [JsonIgnore] public AABB<Vec3<TNum>> BBox => AABB<Vec3<TNum>>.From(A, B, C);

    [System.Diagnostics.Contracts.Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Triangle3<TOtherNum> To<TOtherNum>()
        where TOtherNum : unmanaged, IFloatingPointIeee754<TOtherNum> =>
        new(A.To<TOtherNum>(), B.To<TOtherNum>(), C.To<TOtherNum>());

    [System.Diagnostics.Contracts.Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Triangle3<TNum> Transform(Mat4x4<TNum> m, Triangle3<TNum> t)
        => new(Mat4x4<TNum>.MultiplyPoint(m, t.A), Mat4x4<TNum>.MultiplyPoint(m, t.B),
            Mat4x4<TNum>.MultiplyPoint(m, t.C));

    public static bool DoIntersect(Triangle3<TNum> a, Triangle3<TNum> b)
    {
        var pA = a.Plane;
        if (!pA.DoIntersect(b))
            return false;

        var pB = b.Plane;
        if (!pB.DoIntersect(a))
            return false;

        var dir = pA.Normal.Cross(pB.Normal);
        var ray = a.A.RayThrough(dir);
        var intervalA = ProjectOnto(a, ray);
        var intervalB = ProjectOnto(b, ray);
        return intervalA.IntersectsWith(intervalB);
    }

    public static bool DoIntersect(
        Triangle3<TNum> tri,
        AABB<Vec3<TNum>> box)
    {
        var center = box.Center;
        var extents = box.Size * Numbers<TNum>.Half;

        var v0 = tri.A - center;
        var v1 = tri.B - center;
        var v2 = tri.C - center;

        var e0 = v1 - v0;
        var e1 = v2 - v1;
        var e2 = v0 - v2;

        var loc = AABB.From(v0, v1, v2);
        var compAgainst = new AABB<Vec3<TNum>>(-extents, extents);
        if (!compAgainst.IntersectsWith(loc)) return false;
        var plane = tri.Plane;
        if (!plane.DoIntersect(box))
            return false;

        return TestEdge(e0, v0, v1, v2, extents)
               && TestEdge(e1, v0, v1, v2, extents)
               && TestEdge(e2, v0, v1, v2, extents);
    }

    static bool TestEdge(
        Vec3<TNum> edge,
        Vec3<TNum> v0,
        Vec3<TNum> v1,
        Vec3<TNum> v2,
        Vec3<TNum> extents)
    {
        return AxisTest(Vec3<TNum>.Create(TNum.Zero, -edge.Z, edge.Y), v0, v1, v2, extents)
               && AxisTest(Vec3<TNum>.Create(edge.Z, TNum.Zero, -edge.X), v0, v1, v2, extents)
               && AxisTest(Vec3<TNum>.Create(-edge.Y, edge.X, TNum.Zero), v0, v1, v2, extents);
    }

    static bool AxisTest(
        Vec3<TNum> axis,
        Vec3<TNum> v0,
        Vec3<TNum> v1,
        Vec3<TNum> v2,
        Vec3<TNum> extents)
    {
        if (axis.SquaredLength.IsApproxZero())
            return true; // Degenerate axis, skip

        var p0 = Vec3<TNum>.Dot(v0, axis);
        var p1 = Vec3<TNum>.Dot(v1, axis);
        var p2 = Vec3<TNum>.Dot(v2, axis);

        var min = TNum.Min(p0, TNum.Min(p1, p2));
        var max = TNum.Max(p0, TNum.Max(p1, p2));

        var r =
            extents.X * TNum.Abs(axis.X) +
            extents.Y * TNum.Abs(axis.Y) +
            extents.Z * TNum.Abs(axis.Z);

        if (min > r || max < -r)
            return false;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AABB<TNum> ProjectOnto(Triangle3<TNum> tri, Ray3<TNum> line) =>
        AABB.From(line.ParameterOfClosestPoint(tri.A),
            line.ParameterOfClosestPoint(tri.B),
            line.ParameterOfClosestPoint(tri.C));

    public static bool DoIntersectOrTouch(Triangle3<TNum> a, Triangle3<TNum> b)
    {
        var pA = a.Plane;
        var pB = b.Plane;

        var nA = pA.Normal;
        var nB = pB.Normal;


        if (TNum.Abs(Vec3<TNum>.Dot(pA.Normal, pB.Normal)).IsApprox(TNum.One))
            return Plane<TNum>.AreCoplanar(pA, pB) && CoplanarTriangleTest(a, b, pA);
        if (!pB.DoIntersect(a)) return false;
        if (!pA.DoIntersect(b)) return false;

        var dir = Vec3<TNum>.Cross(nA, nB);

        var ray = Vec3<TNum>.Zero.RayAlong(dir);
        var (minA, maxA) = ProjectOnto(a, ray);
        var (minB, maxB) = ProjectOnto(b, ray);

        return !(maxA < minB || maxB < minA);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CoplanarTriangleTest(Triangle3<TNum> a, Triangle3<TNum> b, Plane<TNum> testPlane)
    {
        return Triangle2<TNum>.DoIntersect(testPlane.ProjectIntoLocal(a), testPlane.ProjectIntoLocal(b));
    }

    /// <inheritdoc />
    public bool Equals(Triangle3<TNum> other)
    {
        return A.Equals(other.A) && B.Equals(other.B) && C.Equals(other.C);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Triangle3<TNum> other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(A, B, C);
    }

    public static bool operator ==(Triangle3<TNum> left, Triangle3<TNum> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Triangle3<TNum> left, Triangle3<TNum> right)
    {
        return !left.Equals(right);
    }
}