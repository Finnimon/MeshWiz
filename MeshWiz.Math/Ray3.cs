using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Ray3<TNum>
    : IIntersecter<Triangle3<TNum>, TNum>,
        IIntersecter<Plane3<TNum>, TNum>,
        IIntersecter<AABB<Vec3<TNum>>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Vec3<TNum> Origin, Direction;

    public Ray3(Vec3<TNum> origin, Vec3<TNum> direction)
    {
        Origin = origin;
        Direction = direction.Normalized();
    }

    private Ray3(Vec3<TNum> origin, Vec3<TNum> direction, bool _)
    {
#if DEBUG
        if(!direction.IsNormalized)
            ThrowHelper.ThrowArgumentException("Parameter direction must already be normal for unsafe creation");
#endif
        Origin = origin;
        Direction = direction;
    }

    public static Ray3<TNum> UnitX => CreateUnsafe(Vec3<TNum>.Zero, Vec3<TNum>.UnitX);
    public static Ray3<TNum> UnitY => CreateUnsafe(Vec3<TNum>.Zero, Vec3<TNum>.UnitY);
    public static Ray3<TNum> UnitZ => CreateUnsafe(Vec3<TNum>.Zero, Vec3<TNum>.UnitZ);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3<TNum> Traverse(TNum distance)
        => Origin + Direction * distance;

    public static Vec3<TNum> operator *(in Ray3<TNum> ray, TNum distance) => ray.Traverse(distance);
    public static Vec3<TNum> operator *(TNum distance, in Ray3<TNum> ray) => ray.Traverse(distance);


    public bool DoIntersect(Plane3<TNum> plane)
    {
        var dot = plane.Normal.Dot(Direction);
        return dot > TNum.Epsilon;
    }

    public bool Intersect(Plane3<TNum> plane, out TNum t)
    {
        var denominator = plane.Normal.Dot(Direction);

        // Check if ray is parallel to the plane
        if (TNum.Abs(denominator) < TNum.Epsilon)
        {
            t = TNum.NaN;
            return false;
        }

        // Compute intersection distance along ray direction
        t = -(plane.Normal.Dot(Origin) + plane.D) / denominator;

        // If t < 0, the intersection point is behind the ray's origin
        return t >= TNum.Zero;
    }

    public bool Intersect(Triangle3<TNum> triangle, out TNum t)
    {
        t = TNum.NaN;

        var edge1 = triangle.B - triangle.A;
        var edge2 = triangle.C - triangle.A;
        var h = Vec3<TNum>.Cross(Direction, edge2); // cross product
        var a = edge1.Dot(h); // dot product

        if (TNum.Abs(a) < TNum.Epsilon)
            return false; // Ray is parallel to the triangle

        var f = TNum.One / a;
        var s = Origin - triangle.A;
        var u = f * (s.Dot(h));

        if (u < TNum.Zero || u > TNum.One)
            return false;

        var q = Vec3<TNum>.Cross(s, edge1);
        var v = f * (Direction.Dot(q));

        if (v < TNum.Zero || u + v > TNum.One)
            return false;

        t = f * (edge2.Dot(q));

        return t >= TNum.Zero; // Intersection in ray direction
    }

    public bool HitTest(AABB<Vec3<TNum>> box, out TNum tNear, out TNum tFar)
    {
        tNear = TNum.NegativeInfinity;
        tFar = TNum.PositiveInfinity;

        for (int i = 0; i < 3; i++)
        {
            var origin = Origin[i];
            var dir = Direction[i];
            var min = box.Min[i];
            var max = box.Max[i];

            if (TNum.Abs(dir) < TNum.Epsilon && (origin < min || origin > max)) return false;
            var invD = TNum.One / dir;
            var t1 = (min - origin) * invD;
            var t2 = (max - origin) * invD;
            if (t1 > t2) (t1, t2) = (t2, t1);

            tNear = TNum.Max(tNear, t1);
            tFar = TNum.Min(tFar, t2);

            if (tNear > tFar || tFar < TNum.Zero) return false;
        }

        return true;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Ray3<TNum>(Line<Vec3<TNum>, TNum> line)
        => new(line.Start, line.AxisVector);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Line<Vec3<TNum>, TNum>(Ray3<TNum> ray)
        => new(ray.Origin, ray.Direction + ray.Origin);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3<TNum> ClosestPoint(Vec3<TNum> p)
    {
        var v = p - Origin;
        var ndir = Direction;
        var dotProduct = v.Dot(ndir);
        var alongVector = dotProduct * ndir;
        return Origin + alongVector;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum DistanceTo(Vec3<TNum> p) => ClosestPoint(p).DistanceTo(p);


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool DoIntersect(Triangle3<TNum> test)
        => Intersect(test, out _);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool DoIntersect(AABB<Vec3<TNum>> test)
        => Intersect(test, out _);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Intersect(AABB<Vec3<TNum>> test, out TNum result)
        => HitTest(test, out result, out _);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Line<Vec3<TNum>, TNum> LineSection(TNum start, TNum end)
        => new(Origin + Direction * start, Origin + Direction * end);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ray3<TNum> CreateUnsafe(Vec3<TNum> origin, Vec3<TNum> direction) =>
        new(origin, direction, true);
}