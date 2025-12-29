using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct ConeGeodesic<TNum> : IDiscretePoseCurve<Pose3<TNum>, Vec3<TNum>, TNum>,
    IEquatable<ConeGeodesic<TNum>>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Cone<TNum> Surface;
    public readonly Vec2<TNum> PolarStart, PolarEnd;
    public TNum AngleRangeSize => (Surface.Radius / Surface.SlantHeight) * Numbers<TNum>.TwoPi;
    public AABB<TNum> AngleRange => AABB.Around(TNum.Zero, AngleRangeSize);

    public ConeGeodesic(in Cone<TNum> surface, Vec2<TNum> polarStart, Vec2<TNum> polarEnd)
    {
        Surface = surface;
        PolarStart = polarStart;
        PolarEnd = polarEnd;
    }

    public static ConeGeodesic<TNum> BetweenPoints(in Cone<TNum> surface, Vec3<TNum> p1, Vec3<TNum> p2)
    {
        if (p1.IsApprox(p2)) ThrowHelper.ThrowArgumentException(nameof(p2), "Param points are equal");
        var projP1 = surface.ProjectPoint(p1);
        var p1Polar = projP1.CartesianToPolar();
        var projP2 = surface.ProjectPoint(p2);
        var p2Polar = projP2.CartesianToPolar();

        if (projP1.IsApprox(projP2))
            ThrowHelper.ThrowArgumentException(nameof(p2), "Param points are too close when projected");
        var fullRotation = Numbers<TNum>.TwoPi / GetRelativeAngleSpace(in surface);
        var p2Plus = p2Polar.PolarAngle + fullRotation;
        var p2Minus = p2Polar.PolarAngle;

        var neutralDistance = TNum.Abs(p1Polar.PolarAngle - p2Polar.PolarAngle);
        var plusDistance = TNum.Abs(p1Polar.PolarAngle - p2Plus);
        var minusDistance = TNum.Abs(p1Polar.PolarAngle - p2Minus);

        if (plusDistance < minusDistance && plusDistance < neutralDistance)
            p2Polar = new Vec2<TNum>(p2Polar.PolarRadius, p2Plus);
        else if (minusDistance < plusDistance && minusDistance < neutralDistance)
            p2Polar = new Vec2<TNum>(p2Polar.PolarRadius, p2Minus);

        return new ConeGeodesic<TNum>(in surface, p1Polar, p2Polar);
    }

    public static ConeGeodesic<TNum> FromDirection(ConeSection<TNum> surface, Vec3<TNum> p, Vec3<TNum> dir)
    {
        var cylindrical = !surface.TryGetComplete(out var cone);
        if (cylindrical || surface.IsComplex) ThrowHelper.ThrowInvalidOperationException();
        p = surface.ClampToSurface(p);
        var proj = cone.ProjectDirection(p, dir);
        if (Vec3<TNum>.IsNaN(dir) || dir.SquaredLength.IsApproxZero())
            ThrowHelper.ThrowArgumentException(nameof(dir));
        var p1Polar = proj.Origin.CartesianToPolar();
        var straightLine = proj.Direction.IsParallelTo(proj.Origin);
        var p2Polar = straightLine
            ? GetStraightLineP2(in surface, in cone, in proj, p1Polar)
            : GetCurvedLineP2(in surface, in cone, in proj, p1Polar);
        return new ConeGeodesic<TNum>(in cone, p1Polar, p2Polar);
    }


    private static Vec2<TNum> GetCurvedLineP2(in ConeSection<TNum> surface, in Cone<TNum> complete,
        in Ray2<TNum> proj, Vec2<TNum> p1Polar)
    {
        var (major, minor) = ExtractProjectedCircles(in surface, in complete);
        var foundIntersection = proj.TryIntersect(major, out var t1, out var t2);
        foundIntersection |= proj.TryIntersect(minor, out var t3, out var t4);
        if (!foundIntersection) ThrowHelper.ThrowInvalidOperationException();

        TNum[] parameters = [t1, t2, t3, t4];
        parameters = parameters.Where(TNum.IsRealNumber)
            .Where(parameter => !TNum.Zero.IsApproxGreaterOrEqual(parameter))
            .Order().ToArray();
        if (parameters.Length == 0)
            ThrowHelper.ThrowInvalidOperationException($"Direction into {nameof(surface)} was only tangential");
        var t = parameters[0];
        var p2 = proj.Traverse(t);
        var p2Polar = p2.CartesianToPolar();
        var angleDiff = p1Polar.PolarAngle - p2Polar.PolarAngle;
        if (TNum.Pi.IsApproxGreaterOrEqual(TNum.Abs(angleDiff)))
            return p2Polar;
        var shift = TNum.CopySign(Numbers<TNum>.TwoPi, angleDiff);
        return Vec2<TNum>.CreatePolar(p2Polar.PolarRadius, p2Polar.PolarAngle + shift);
    }

    private static (Circle2<TNum> major, Circle2<TNum> minor) ExtractProjectedCircles(in ConeSection<TNum> surface,
        in Cone<TNum> cone)
    {
        var slantHeight = cone.SlantHeight;
        Circle2<TNum> major = new(Vec2<TNum>.Zero, slantHeight);
        var factor = surface.TopRadius / surface.BaseRadius;
        Circle2<TNum> minor = new(Vec2<TNum>.Zero, slantHeight * factor);
        return (major, minor);
    }

    private static Vec2<TNum> GetStraightLineP2(in ConeSection<TNum> surface, in Cone<TNum> complete,
        in Ray2<TNum> proj, Vec2<TNum> p1Polar)
    {
        var sign = TNum.Sign(proj.Direction.Dot(proj.Origin));
        var toTip = 0 > sign;
        var p2Radius = toTip ? complete.SlantHeight - surface.SlantHeight : complete.SlantHeight;
        var p2Polar = Vec2<TNum>.CreatePolar(p2Radius, p1Polar.PolarAngle);
        return p2Polar;
    }

    public static ConeGeodesic<TNum> FromDirection(in Cone<TNum> surface, Vec3<TNum> p, Vec3<TNum> dir)
    {
        p = surface.ClampToSurface(p);
        var fromTip = p.IsApprox(surface.Tip);
        if (fromTip)
            return SpecialCaseFromTip(surface, dir);
        var proj = surface.ProjectDirection(p, dir);
        if (Vec3<TNum>.IsNaN(dir) || dir.SquaredLength.IsApproxZero())
            ThrowHelper.ThrowArgumentException(nameof(dir));
        var p1Polar = proj.Origin.CartesianToPolar();
        var straightLine = proj.Direction.IsParallelTo(proj.Origin);
        Vec2<TNum> p2Polar;
        if (straightLine)
            p2Polar = GetStraightLineP2(in surface, in proj, p1Polar);
        else
            p2Polar = GetCurvedLineP2(in surface, in proj, p1Polar);
        return new ConeGeodesic<TNum>(in surface, p1Polar, p2Polar);
    }

    private static ConeGeodesic<TNum> SpecialCaseFromTip(in Cone<TNum> surface, in Vec3<TNum> dir)
    {
        if (dir.IsParallelTo(surface.Axis.AxisVector))
            ThrowHelper.ThrowArgumentException(nameof(dir),
                "Geodesic can not start at tip with dir parallel to surface.Axis");
        var baseC = surface.Base;
        var dirOnC = baseC.ClampToSurface(dir + baseC.Centroid);
        var realP2 = Vec3<TNum>.ExactLerp(baseC.Centroid, dirOnC, baseC.Radius);
        return BetweenPoints(in surface, surface.Tip, realP2);
    }

    private static Vec2<TNum> GetCurvedLineP2(in Cone<TNum> surface, in Ray2<TNum> proj, Vec2<TNum> p1Polar)
    {
        var bounds = ExtractProjectedCircle(surface);
        var foundIntersection = proj.TryIntersect(bounds, out var t1, out var t2);
        if (!foundIntersection) ThrowHelper.ThrowInvalidOperationException();
        TNum t;
        if (!TNum.Zero.IsApproxGreaterOrEqual(t1)) t = t1;
        else if (!TNum.Zero.IsApproxGreaterOrEqual(t2)) t = t2;
        else return ThrowHelper.ThrowInvalidOperationException<Vec2<TNum>>();
        var p2 = proj.Traverse(t);
        var p2Polar = p2.CartesianToPolar();
        var angleDiff = p1Polar.PolarAngle - p2Polar.PolarAngle;
        if (TNum.Pi.IsApproxGreaterOrEqual(TNum.Abs(angleDiff)))
            return p2Polar;
        var shift = TNum.CopySign(Numbers<TNum>.TwoPi, angleDiff);
        return Vec2<TNum>.CreatePolar(p2Polar.PolarRadius, p2Polar.PolarAngle + shift);
    }

    [Pure]
    private static Vec2<TNum> GetStraightLineP2(in Cone<TNum> surface, in Ray2<TNum> proj, Vec2<TNum> p1Polar)
    {
        var sign = TNum.Sign(proj.Direction.Dot(proj.Origin));
        var toTip = 0 > sign;
        var p2Radius = toTip ? TNum.Zero : surface.SlantHeight;
        var p2Polar = Vec2<TNum>.CreatePolar(p2Radius, p1Polar.PolarAngle);
        return p2Polar;
    }


    [Pure]
    public Vec3<TNum> Traverse(TNum t)
    {
        var polar = Vec2<TNum>.PolarLerp(PolarStart, PolarEnd, t);
        var p = ProjectPolarPoint(in Surface, polar);
        return p;
    }

    [Pure]
    public static TNum GetRelativeAngleSpace(in Cone<TNum> surface) => surface.SlantHeight / surface.Radius;

    [Pure]
    public static Circle2<TNum> ExtractProjectedCircle(in Cone<TNum> surface) =>
        new(Vec2<TNum>.Zero, surface.SlantHeight);


    /// <inheritdoc />
    [Pure]
    public Vec3<TNum> Start => ProjectPolarPoint(in Surface, PolarStart);

    /// <inheritdoc />
    public Pose3<TNum> EndPose => GetPose(TNum.One);

    /// <inheritdoc />
    public Pose3<TNum> StartPose => GetPose(TNum.Zero);

    /// <inheritdoc />
    [Pure]
    public Vec3<TNum> End => ProjectPolarPoint(in Surface, PolarEnd);

    /// <inheritdoc />
    [Pure]
    public Vec3<TNum> TraverseOnCurve(TNum t) => Traverse(TNum.Clamp(t, TNum.Zero, TNum.One));


    /// <inheritdoc />
    public TNum Length => Vec2<TNum>.PolarDistance(PolarStart, PolarEnd);

    /// <inheritdoc />
    [Pure]
    public Polyline<Vec3<TNum>, TNum> ToPolyline()
        => ToPolyline(new PolylineTessellationParameter<TNum>
            { MaxAngularDeviation = Numbers<TNum>.Eps2 * Numbers<TNum>.TwoPi });


    /// <inheritdoc />
    [Pure]
    public Polyline<Vec3<TNum>, TNum> ToPolyline(PolylineTessellationParameter<TNum> tessellationParameter)
        => new(GetPolylineSteps(tessellationParameter).Select(Traverse));

    private IEnumerable<TNum> GetPolylineSteps(PolylineTessellationParameter<TNum> tessellationParameter)
    {
        var angleRange = AngleRange.Size;
        if (angleRange.IsApproxZero())
            return [TNum.Zero, TNum.One];
        var (count, countNum, _) = tessellationParameter.GetStepsForAngle(angleRange);
        var stepSize = TNum.One / (countNum);
        return Enumerable.Range(0, count + 1).Select(i => TNum.CreateTruncating(i) * stepSize);
    }

    [Pure]
    public static Vec3<TNum> ProjectPolarPoint(in Cone<TNum> surface, Vec2<TNum> polar)
    {
        var slantHeight = surface.SlantHeight;
        var space = slantHeight / surface.Radius;
        var trueAngle = polar.PolarAngle * space;
        var basePt = surface.Base.TraverseByAngle(trueAngle);
        var lerpFactor = polar.PolarRadius / slantHeight;
        return Vec3<TNum>.Lerp(surface.Tip, basePt, lerpFactor);
    }

    /// <inheritdoc />
    public Vec3<TNum> GetTangent(TNum t)
        => GetRay(t).Direction;

    public Vec2<TNum> CartesianStart => PolarStart.PolarToCartesian();
    public Vec2<TNum> CartesianEnd => PolarEnd.PolarToCartesian();
    public Vec2<TNum> CartesianAxisVector => (CartesianEnd - CartesianStart);
    public Vec2<TNum> CartesianDirection => (CartesianEnd - CartesianStart).Normalized();

    /// <inheritdoc />
    public Vec3<TNum> EntryDirection => GetTangent(TNum.Zero);

    /// <inheritdoc />
    public Vec3<TNum> ExitDirection => GetTangent(TNum.One);

    /// <inheritdoc />
    public PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum> ToPosePolyline()
        => ToPosePolyline(new PolylineTessellationParameter<TNum>
            { MaxAngularDeviation = Numbers<TNum>.Eps2 * Numbers<TNum>.TwoPi });

    /// <inheritdoc />
    public PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum> ToPosePolyline(
        PolylineTessellationParameter<TNum> tessellationParameter)
        => new(GetPolylineSteps(tessellationParameter).Select(GetPose));


    /// <inheritdoc />
    public bool Equals(ConeGeodesic<TNum> other) => Surface.Equals(other.Surface) &&
                                                    PolarStart.Equals(other.PolarStart) &&
                                                    PolarEnd.Equals(other.PolarEnd);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ConeGeodesic<TNum> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Surface, PolarStart, PolarEnd);

    public static bool operator ==(ConeGeodesic<TNum> left, ConeGeodesic<TNum> right) => left.Equals(right);

    public static bool operator !=(ConeGeodesic<TNum> left, ConeGeodesic<TNum> right) => !left.Equals(right);

    public Ray3<TNum> GetRay(TNum t)
    {
        var specialCaseStraightLine = IsStraightLine();
        if (specialCaseStraightLine) return Start.RayThrough(End);
        var (p, dir, _) = GetPoseData(t);
        return new Ray3<TNum>(p, dir);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsStraightLine() =>
        PolarStart.PolarRadius.IsApproxZero()
        || PolarEnd.PolarRadius.IsApproxZero()
        || PolarStart.PolarAngle.IsApprox(PolarEnd.PolarAngle);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pose3<TNum> GetPose(TNum t)
    {
        if (IsStraightLine())
            return GetPoseStraightLine();
        var (p, dir, normal) = GetPoseData(t);
        return Pose3<TNum>.CreateUnsafe(p, dir, normal);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (Vec3<TNum> p, Vec3<TNum> dir, Vec3<TNum> normal) GetPoseData(TNum t)
    {
        var polarP = Vec2<TNum>.PolarLerp(PolarStart, PolarEnd, t);
        var p = ProjectPolarPoint(in Surface, polarP);
        var pAngle = polarP.PolarAngle;
        var tipToP = p - Surface.Tip;
        var normal = Surface.NormalAtUnsafe(p);
        var polarAxis = CartesianAxisVector.CartesianToPolar();
        var dirAngle = polarAxis.PolarAngle - pAngle;
        var rot = Matrix3x3<TNum>.CreateRotation(normal, dirAngle);
        var dir = rot * tipToP;
        return (p, dir, normal);
    }

    private Pose3<TNum> GetPoseStraightLine()
    {
        var start = Start;
        var end = End;
        var sPt = Vec3<TNum>.Lerp(start, end, Numbers<TNum>.Half);
        var up = Surface.NormalAtUnsafe(sPt);
        return Pose3<TNum>.CreateUnsafe(sPt, end - start, up);
    }

    public ConeGeodesic<TNum> Section(TNum start, TNum end)
    {
        var startPt = Vec2<TNum>.PolarLerp(PolarStart, PolarEnd, start);
        var endPt = Vec2<TNum>.PolarLerp(PolarStart, PolarEnd, end);
        return new ConeGeodesic<TNum>(Surface, startPt, endPt);
    }
}