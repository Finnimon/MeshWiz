using System.Diagnostics.Contracts;
using System.Numerics;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public readonly struct Cone<TNum> : IBody<TNum>, IRotationalSurface<TNum>, IEquatable<Cone<TNum>>,
    IGeodesicProvider<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Line<Vector3<TNum>, TNum> Axis;
    public readonly TNum Radius;
    public TNum SlantHeight => TNum.Sqrt(Radius * Radius + Axis.SquaredLength);

    public Cone(Line<Vector3<TNum>, TNum> axis, TNum radius)
    {
        if (TNum.IsPositive(radius))
        {
            Axis = axis;
            Radius = radius;
        }
        else
        {
            Axis = axis.Reversed();
            Radius = -radius;
        }
    }

    public Vector3<TNum> Centroid => Axis.Traverse(Numbers<TNum>.Fourth);
    public TNum Volume => Axis.Length * Base.SurfaceArea * Numbers<TNum>.Third;
    public Vector3<TNum> Tip => Axis.End;
    public Circle3<TNum> Base => new(Axis.Start, Axis.Direction, Radius);

    /// <inheritdoc />
    public TNum SurfaceArea => Base.SurfaceArea + OpenConeSurfaceArea(Radius, Axis.Length);

    public static TNum OpenConeSurfaceArea(TNum radius, TNum height)
    {
        var s = TNum.Sqrt(radius * radius + height * height);
        return radius * s * TNum.Pi;
    }

    /// <returns><see cref="Base"/> projected along <see cref="Axis"/> by <paramref name="normHeight"/></returns>
    public Circle3<TNum> GetCircleAt(TNum normHeight)
    {
        var c = Axis.Traverse(normHeight);
        var radius = GetRadiusAt(normHeight);
        return new Circle3<TNum>(c, Axis.Direction, radius);
    }

    public TNum GetRadiusAt(TNum normHeight) => TNum.Lerp(Radius, TNum.Zero, normHeight);

    /// <inheritdoc />
    public AABB<Vector3<TNum>> BBox => Base.BBox.CombineWith(Tip);

    /// <inheritdoc />
    public IMesh<TNum> Tessellate() => Tessellate(32);

    public IndexedMesh<TNum> Tessellate(int edgeCount)
    {
        var baseMesh = Base.Reversed().Tessellate(edgeCount).Indexed();
        var indices = new TriangleIndexer[edgeCount * 2];
        baseMesh.Indices.CopyTo(indices, 0);
        Vector3<TNum>[] vertices = [..baseMesh.Vertices, Tip];
        var tipIndex = vertices.Length - 1;
        for (var i = 0; i < edgeCount; i++)
        {
            var baseIndexer = indices[i];
            indices[i + edgeCount] = new TriangleIndexer(tipIndex, baseIndexer.C, baseIndexer.B);
        }

        return new IndexedMesh<TNum>(vertices, indices);
    }

    public ConeSection<TNum> Section(TNum start, TNum end)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(start, end);
        if (start > end) (start, end) = (end, start);
        var axis = Axis.Section(start, end);
        var baseRadius = GetRadiusAt(start);
        var topRadius = GetRadiusAt(end);
        return new ConeSection<TNum>(axis, baseRadius, topRadius);
    }

    /// <inheritdoc />
    public IDiscreteCurve<Vector3<TNum>, TNum> SweepCurve => Base.Traverse(TNum.Zero).LineTo(Tip);

    /// <inheritdoc />
    public Ray3<TNum> SweepAxis => Axis.Start.RayThrough(Axis.End);

    /// <inheritdoc />
    public Vector3<TNum> NormalAt(Vector3<TNum> p)
    {
        var baseC = Base;
        p = ClampToSurface(p);
        var p2 = baseC.Plane.ProjectIntoLocal(p - baseC.Centroid);
        if (p2.IsApproxZero()) return Axis.NormalDirection;
        var anglePos = p2.CartesianToPolar().PolarAngle;
        var tangent = baseC.GetTangentAtAngle(anglePos);
        var pToTip = Tip - p;
        return tangent.Cross(pToTip).Normalized;
        // var closest = Axis.ClosestPoint(p);
        // var axisToP = (p - closest).Normalized;
        // var axisN = Axis.Direction;
        // var axisLen = axisN.Length;
        // axisN /= axisLen;
        //
        // // Degenerate case (tip)
        // if (!axisToP.IsNormalized)
        //     return axisN;
        //
        // // Half-angle of cone (tip-to-base)
        // var apexAngle = TNum.Atan(Radius / axisLen);
        // var (sin, cos) = TNum.SinCos(apexAngle);
        //
        // // Note the minus sign for outward normal
        // return (cos * axisN - sin * axisToP).Normalized;
    }


    public TNum ApexAngle => TNum.Atan(Radius / Axis.Length);

    [Pure]
    public Vector3<TNum> ClampToSurface(Vector3<TNum> p)
    {
        var (closest, onSeg) = Axis.ClosestPoints(p);
        p += onSeg - closest;
        var pos = onSeg.DistanceTo(Axis.Start) / Axis.Length;
        var radius = TNum.Abs(RadiusAt(pos));
        return Vector3<TNum>.ExactLerp(onSeg, p, radius);
    }

    public TNum RadiusAt(TNum pos) => TNum.Lerp(Radius, TNum.Zero, pos);


    [Pure]
    public Vector2<TNum> ProjectPoint(Vector3<TNum> p)
    {
        p = ClampToSurface(p);
        if (p.IsApprox(Tip)) return Vector2<TNum>.Zero;
        var baseCircle = Base;
        var basis = baseCircle.Basis;
        var baseToP = p - Axis.Start;
        var angle = Vector3<TNum>.SignedAngleBetween(basis.U, baseToP, baseCircle.Normal);
        if (TNum.IsNaN(angle)) return Vector2<TNum>.Zero; //no angle means at tip but not detected via is Approx
        var relativeAngleSpace = baseCircle.Radius / SlantHeight;
        var realAngle = angle * relativeAngleSpace;
        var polarRadius = p.DistanceTo(Tip);
        Vector2<TNum> polar = new(polarRadius, realAngle);
        return polar.PolarToCartesian();
    }

    [Pure]
    public Vector3<TNum> ProjectPoint(Vector2<TNum> p)
    {
        var polarPt = p.CartesianToPolar();
        var baseAngle = polarPt.PolarAngle;
        var coneTip = Tip;
        var baseCircle = Base;
        var projRadius = SlantHeight;
        var relativeAngleSpace = projRadius / baseCircle.Radius;
        var realAngle = relativeAngleSpace * baseAngle;
        var tipToBase = polarPt.PolarRadius / projRadius;
        var basePt = baseCircle.TraverseByAngle(realAngle);
        return Vector3<TNum>.Lerp(coneTip, basePt, tipToBase);
    }

    [Pure]
    public Ray2<TNum> ProjectDirection(Vector3<TNum> p, Vector3<TNum> direction)
    {
        var origin = ClampToSurface(p);
        var onSurface = ProjectPoint(p);
        var normal = NormalAt(p);
        var tipToOrigin = origin - Tip;
        var angle = Vector3<TNum>.SignedAngleBetween(tipToOrigin, direction, normal);
        var polarOnSurface = onSurface.CartesianToPolar();
        Vector2<TNum> projectedDir = new(TNum.One, polarOnSurface.PolarAngle + angle);
        projectedDir = projectedDir.PolarToCartesian();
        return new Ray2<TNum>(onSurface, projectedDir);
    }

    /// <inheritdoc />
    public bool Equals(Cone<TNum> other)
    {
        return Axis.Equals(other.Axis) && Radius.Equals(other.Radius);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Cone<TNum> other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Axis, Radius);
    }

    /// <inheritdoc />
    public IContiguousCurve<Vector3<TNum>, TNum> GetGeodesic(Vector3<TNum> p1, Vector3<TNum> p2)
        => ConeGeodesic<TNum>.BetweenPoints(in this, p1, p2);

    /// <inheritdoc />
    public IContiguousCurve<Vector3<TNum>, TNum> GetGeodesicFromEntry(Vector3<TNum> entryPoint, Vector3<TNum> direction)
        => ConeGeodesic<TNum>.FromDirection(in this, entryPoint, direction);
}

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
        parameters = parameters.Where(TNum.IsRealNumber).Where(parameter => !TNum.Zero.IsApproxGreaterOrEqual(parameter))
            .Order().ToArray();
        if(parameters.Length==0) ThrowHelper.ThrowInvalidOperationException($"Direction into {nameof(surface)} was only tangential");
            var t=parameters[0];
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
        var proj = surface.ProjectDirection(p, dir);
        if (Vector3<TNum>.IsNaN(dir) || dir.SquaredLength.IsApproxZero())
            ThrowHelper.ThrowArgumentException(nameof(dir));
        var p1Polar = proj.Origin.CartesianToPolar();
        var straightLine = proj.Direction.IsParallelTo(proj.Origin);
        var p2Polar = straightLine
            ? GetStraightLineP2(in surface, in proj, p1Polar)
            : GetCurvedLineP2(in surface, in proj, p1Polar);
        return new ConeGeodesic<TNum>(in surface, p1Polar, p2Polar);
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