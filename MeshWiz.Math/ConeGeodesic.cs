using System.Diagnostics.Contracts;
using System.Numerics;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public readonly struct ConeGeodesic<TNum> : IContiguousDiscreteCurve<Vector3<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Cone<TNum> Surface;
    public readonly Vector2<TNum> PolarStart, PolarEnd;
    public TNum AngleRangeSize => (Surface.Radius / Surface.SlantHeight) * Numbers<TNum>.TwoPi;
    public AABB<TNum> AngleRange => AABB.Around(TNum.Zero, AngleRangeSize);

    public ConeGeodesic(in Cone<TNum> surface, Vector2<TNum> polarStart, Vector2<TNum> polarEnd)
    {
        Surface = surface;
        PolarStart = polarStart;
        PolarEnd = polarEnd;
    }

    public static ConeGeodesic<TNum> BetweenPoints(in Cone<TNum> surface, Vector3<TNum> p1, Vector3<TNum> p2)
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
            p2Polar = new Vector2<TNum>(p2Polar.PolarRadius, p2Plus);
        else if (minusDistance < plusDistance && minusDistance < neutralDistance)
            p2Polar = new Vector2<TNum>(p2Polar.PolarRadius, p2Minus);

        return new ConeGeodesic<TNum>(in surface, p1Polar, p2Polar);
    }

    public static ConeGeodesic<TNum> FromDirection(ConeSection<TNum> surface, Vector3<TNum> p, Vector3<TNum> dir)
    {
        var cylindrical = !surface.TryGetComplete(out var cone);
        if (cylindrical || surface.IsComplex) ThrowHelper.ThrowInvalidOperationException();
        p = surface.ClampToSurface(p);
        var proj = cone.ProjectDirection(p, dir);
        if (Vector3<TNum>.IsNaN(dir) || dir.SquaredLength.IsApproxZero())
            ThrowHelper.ThrowArgumentException(nameof(dir));
        var p1Polar = proj.Origin.CartesianToPolar();
        var straightLine = proj.Direction.IsParallelTo(proj.Origin);
        var p2Polar = straightLine
            ? GetStraightLineP2(in surface, in cone, in proj, p1Polar)
            : GetCurvedLineP2(in surface, in cone, in proj, p1Polar);
        return new ConeGeodesic<TNum>(in cone, p1Polar, p2Polar);
    }


    private static Vector2<TNum> GetCurvedLineP2(in ConeSection<TNum> surface, in Cone<TNum> complete,
        in Ray2<TNum> proj, Vector2<TNum> p1Polar)
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
        return Vector2<TNum>.CreatePolar(p2Polar.PolarRadius, p2Polar.PolarAngle + shift);
    }

    private static (Circle2<TNum> major, Circle2<TNum> minor) ExtractProjectedCircles(in ConeSection<TNum> surface,
        in Cone<TNum> cone)
    {
        var slantHeight = cone.SlantHeight;
        Circle2<TNum> major = new(Vector2<TNum>.Zero, slantHeight);
        var factor = surface.TopRadius / surface.BaseRadius;
        Circle2<TNum> minor = new(Vector2<TNum>.Zero, slantHeight * factor);
        return (major, minor);
    }

    private static Vector2<TNum> GetStraightLineP2(in ConeSection<TNum> surface, in Cone<TNum> complete,
        in Ray2<TNum> proj, Vector2<TNum> p1Polar)
    {
        var sign = TNum.Sign(proj.Direction.Dot(proj.Origin));
        var toTip = 0 > sign;
        var p2Radius = toTip ? complete.SlantHeight - surface.SlantHeight : complete.SlantHeight;
        var p2Polar = Vector2<TNum>.CreatePolar(p2Radius, p1Polar.PolarAngle);
        return p2Polar;
    }

    public static ConeGeodesic<TNum> FromDirection(in Cone<TNum> surface, Vector3<TNum> p, Vector3<TNum> dir)
    {
        p = surface.ClampToSurface(p);
        var fromTip = p.IsApprox(surface.Tip);
        if (fromTip)
            return SpecialCaseFromTip(surface, dir);
        var proj = surface.ProjectDirection(p, dir);
        if (Vector3<TNum>.IsNaN(dir) || dir.SquaredLength.IsApproxZero())
            ThrowHelper.ThrowArgumentException(nameof(dir));
        var p1Polar = proj.Origin.CartesianToPolar();
        var straightLine = proj.Direction.IsParallelTo(proj.Origin);
        Vector2<TNum> p2Polar;
        if (straightLine)
            p2Polar = GetStraightLineP2(in surface, in proj, p1Polar);
        else
            p2Polar = GetCurvedLineP2(in surface, in proj, p1Polar);
        return new ConeGeodesic<TNum>(in surface, p1Polar, p2Polar);
    }

    private static ConeGeodesic<TNum> SpecialCaseFromTip(in Cone<TNum> surface, in Vector3<TNum> dir)
    {
        if (dir.IsParallelTo(surface.Axis.Direction))
            ThrowHelper.ThrowArgumentException(nameof(dir),
                "Geodesic can not start at tip with dir parallel to surface.Axis");
        var baseC = surface.Base;
        var dirOnC = baseC.ClampToSurface(dir + baseC.Centroid);
        var realP2 = Vector3<TNum>.ExactLerp(baseC.Centroid, dirOnC, baseC.Radius);
        return BetweenPoints(in surface, surface.Tip, realP2);
    }

    private static Vector2<TNum> GetCurvedLineP2(in Cone<TNum> surface, in Ray2<TNum> proj, Vector2<TNum> p1Polar)
    {
        var bounds = ExtractProjectedCircle(surface);
        var foundIntersection = proj.TryIntersect(bounds, out var t1, out var t2);
        if (!foundIntersection) ThrowHelper.ThrowInvalidOperationException();
        TNum t;
        if (!TNum.Zero.IsApproxGreaterOrEqual(t1)) t = t1;
        else if (!TNum.Zero.IsApproxGreaterOrEqual(t2)) t = t2;
        else return ThrowHelper.ThrowInvalidOperationException<Vector2<TNum>>();
        var p2 = proj.Traverse(t);
        var p2Polar = p2.CartesianToPolar();
        var angleDiff = p1Polar.PolarAngle - p2Polar.PolarAngle;
        if (TNum.Pi.IsApproxGreaterOrEqual(TNum.Abs(angleDiff)))
            return p2Polar;
        var shift = TNum.CopySign(Numbers<TNum>.TwoPi, angleDiff);
        return Vector2<TNum>.CreatePolar(p2Polar.PolarRadius, p2Polar.PolarAngle + shift);
    }

    [Pure]
    private static Vector2<TNum> GetStraightLineP2(in Cone<TNum> surface, in Ray2<TNum> proj, Vector2<TNum> p1Polar)
    {
        var sign = TNum.Sign(proj.Direction.Dot(proj.Origin));
        var toTip = 0 > sign;
        var p2Radius = toTip ? TNum.Zero : surface.SlantHeight;
        var p2Polar = Vector2<TNum>.CreatePolar(p2Radius, p1Polar.PolarAngle);
        return p2Polar;
    }

    [Pure]
    public Vector3<TNum> Traverse(TNum by)
    {
        var polar = Vector2<TNum>.PolarLerp(PolarStart, PolarEnd, by);
        var p = ProjectPolarPoint(in Surface, polar);
        return p;
    }

    [Pure]
    public static TNum GetRelativeAngleSpace(in Cone<TNum> surface) => surface.SlantHeight / surface.Radius;

    [Pure]
    public static Circle2<TNum> ExtractProjectedCircle(in Cone<TNum> surface) =>
        new(Vector2<TNum>.Zero, surface.SlantHeight);


    /// <inheritdoc />
    [Pure]
    public Vector3<TNum> Start => ProjectPolarPoint(in Surface, PolarStart);

    /// <inheritdoc />
    [Pure]
    public Vector3<TNum> End => ProjectPolarPoint(in Surface, PolarEnd);

    /// <inheritdoc />
    [Pure]
    public Vector3<TNum> TraverseOnCurve(TNum distance) => Traverse(TNum.Clamp(distance, TNum.Zero, TNum.One));


    /// <inheritdoc />
    public TNum Length => Vector2<TNum>.PolarDistance(PolarStart, PolarEnd);

    /// <inheritdoc />
    [Pure]
    public Polyline<Vector3<TNum>, TNum> ToPolyline()
    {
        var verts = new Vector3<TNum>[11];
        var pos = TNum.Zero;
        var step = Numbers<TNum>.Eps1;
        for (var i = 0; i < verts.Length; i++)
        {
            verts[i] = Traverse(pos);
            pos += step;
        }

        return new Polyline<Vector3<TNum>, TNum>(verts);
    }

    /// <inheritdoc />
    [Pure]
    public Polyline<Vector3<TNum>, TNum> ToPolyline(PolylineTessellationParameter<TNum> tessellationParameter)
    {
        throw new NotImplementedException();
    }

    [Pure]
    public static Vector3<TNum> ProjectPolarPoint(in Cone<TNum> surface, Vector2<TNum> polar)
    {
        var slantHeight = surface.SlantHeight;
        var space = slantHeight / surface.Radius;
        var trueAngle = polar.PolarAngle * space;
        var basePt = surface.Base.TraverseByAngle(trueAngle);
        var lerpFactor = polar.PolarRadius / slantHeight;
        return Vector3<TNum>.Lerp(surface.Tip, basePt, lerpFactor);
    }

    /// <inheritdoc />
    public Vector3<TNum> GetTangent(TNum at)
    {
        var specialCaseStraightLine = PolarStart.PolarRadius.IsApproxZero()
                                      || PolarEnd.PolarRadius.IsApproxZero()
                                      || PolarStart.PolarAngle.IsApprox(PolarEnd.PolarAngle);
        if (specialCaseStraightLine) return (End - Start).Normalized;
        var polarP = Vector2<TNum>.PolarLerp(PolarStart, PolarEnd, at);
        var p = ProjectPolarPoint(in Surface, polarP);
        var pAngle = polarP.PolarAngle;
        var tipToP = p - Surface.Tip;
        var normal = Surface.NormalAt(p);
        var polarAxis = CartesianAxisVector.CartesianToPolar();
        var dirAngle = polarAxis.PolarAngle - pAngle;
        var rot = Matrix4x4<TNum>.CreateRotation(normal, dirAngle);
        var dir = rot.MultiplyDirection(tipToP);
        dir = dir.Normalized;
        return dir;
    }

    public Vector2<TNum> CartesianStart => PolarStart.PolarToCartesian();
    public Vector2<TNum> CartesianEnd => PolarEnd.PolarToCartesian();
    public Vector2<TNum> CartesianAxisVector => (CartesianEnd - CartesianStart);
    public Vector2<TNum> CartesianDirection => (CartesianEnd - CartesianStart).Normalized;

    /// <inheritdoc />
    public Vector3<TNum> EntryDirection => GetTangent(TNum.Zero);

    /// <inheritdoc />
    public Vector3<TNum> ExitDirection => GetTangent(TNum.One);
}