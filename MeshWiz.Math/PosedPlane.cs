using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using MeshWiz.RefLinq;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public readonly struct PosedPlane<TNum>
    : IAnalyticSurface<TNum>,
        IIntersecter<Line<Vec3<TNum>, TNum>, TNum>,
        IIntersecter<Ray3<TNum>, TNum>,
        IIntersecter<Triangle3<TNum>, Line<Vec3<TNum>, TNum>>,
        IIntersecter<AABB<Vec3<TNum>>, Quad3<TNum>>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Vec3<TNum> Origin, U, V, Normal;
    public readonly TNum D;
    public Plane<TNum> Plane => Plane<TNum>.CreateUnsafe(Normal, D);
    public Pose3<TNum> Pose => Pose3<TNum>.CreateUnsafe(Origin, U, V, Normal);

    public PosedPlane(Vec3<TNum> origin, Quaternion<TNum> rot)
    {
        var mat = rot.AsMatrix3x3();
        U = mat.X;
        V = mat.Y;
        Normal = mat.Z;
        D = -(Vec3<TNum>.Dot(Normal, origin));
        Origin = origin;
    }

    public static PosedPlane<TNum> Create(Pose3<TNum> pose) => new(pose.Origin, pose.Rotation);


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum SignedDistance(Vec3<TNum> p) => Vec3<TNum>.Dot(Normal, p) + D;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int DistanceSign(Vec3<TNum> p) => SignedDistance(p).EpsilonTruncatingSign();

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum DistanceTo(Vec3<TNum> p) => TNum.Abs(SignedDistance(p));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec2<TNum> ProjectIntoLocal(Vec3<TNum> world)
    {
        world -= Origin;
        var x = Vec3<TNum>.Dot(world, U);
        var y = Vec3<TNum>.Dot(world, V);
        return Vec2<TNum>.Create(x, y);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3<TNum> ProjectIntoWorld(Vec2<TNum> local) => Origin + local.X * U + local.Y * V;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec2<TNum>[] ProjectIntoLocal(params ReadOnlySpan<Vec3<TNum>> pts) => pts.Select(ProjectIntoLocal).ToArray();

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3<TNum>[] ProjectIntoWorld(params ReadOnlySpan<Vec2<TNum>> pts) => pts.Select(ProjectIntoWorld).ToArray();

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec2<TNum>[] ProjectIntoLocal(IEnumerable<Vec3<TNum>> pts)
        => pts.TryGetSpan(out var span) ? ProjectIntoLocal(span) : pts.Select(ProjectIntoLocal).ToArray();

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3<TNum>[] ProjectIntoWorld(IEnumerable<Vec2<TNum>> pts)
        => pts.TryGetSpan(out var span) ? ProjectIntoWorld(span) : pts.Select(ProjectIntoWorld).ToArray();


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Polyline<Vec3<TNum>, TNum> ProjectIntoWorld(Polyline<Vec2<TNum>, TNum> pts)
        => Polyline<Vec3<TNum>, TNum>.CreateNonCopying(ProjectIntoWorld(pts.Points));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Polyline<Vec2<TNum>, TNum> ProjectIntoLocal(Polyline<Vec3<TNum>, TNum> pts)
        => Polyline<Vec2<TNum>, TNum>.CreateNonCopying(ProjectIntoLocal(pts.Points));


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3<TNum> Clamp(Vec3<TNum> p)
    {
        var d = SignedDistance(p);
        p -= Normal * d;
        return p;
    }

    TNum IAnalyticSurface<TNum>.CurvatureAt(Vec2<TNum> _) => TNum.Zero;

    /// <inheritdoc />
    TNum IAnalyticSurface<TNum>.CurvatureAt(Vec3<TNum> _) => TNum.Zero;

    bool IAnalyticSurface<TNum>.HasCurvature => false;

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool DoIntersect(Line<Vec3<TNum>, TNum> test) => Plane.DoIntersect(test);

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Intersect(Line<Vec3<TNum>, TNum> test, out TNum t)
    {
        var denominator = Normal.Dot(test.AxisVector);
        if (denominator.IsApproxZero())
        {
            t = default;
            return false;
        }

        t = -SignedDistance(test.Start) / denominator;
        return t.IsApproxGreaterOrEqual(TNum.NegativeZero) && TNum.One.IsApproxGreaterOrEqual(t);
    }

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool DoIntersect(Ray3<TNum> test)
        => Plane.DoIntersect(test);

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Intersect(Ray3<TNum> test, out TNum t)
    {
        Unsafe.SkipInit(out t);
        var denominator = Normal.Dot(test.Direction);
        if ((denominator.IsApproxZero()))
            return false;

        t = -SignedDistance(test.Origin) / denominator;
        return t > TNum.Zero || t.IsApproxZero();
    }

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool DoIntersect(Triangle3<TNum> test)
        => Plane.DoIntersect(test);

    /// <inheritdoc />
    public bool Intersect(Triangle3<TNum> test, out Line<Vec3<TNum>, TNum> result)
        => Plane.Intersect(test, out result);

    /// <inheritdoc />
    public bool DoIntersect(AABB<Vec3<TNum>> test)
        => Plane.DoIntersect(test);

    /// <inheritdoc />
    public bool Intersect(AABB<Vec3<TNum>> test, out Quad3<TNum> result)
        => Plane.Intersect(test, out result);
}