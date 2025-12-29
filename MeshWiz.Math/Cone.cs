using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Cone<TNum> : IBody<TNum>,
    IRotationalSurface<TNum>,
    IEquatable<Cone<TNum>>,
    IGeodesicProvider<ConeGeodesic<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Line<Vec3<TNum>, TNum> Axis;
    public readonly TNum Radius;
    public TNum SlantHeight => TNum.Sqrt(Radius * Radius + Axis.SquaredLength);

    public Cone(Line<Vec3<TNum>, TNum> axis, TNum radius)
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

    public Vec3<TNum> Centroid => Axis.Traverse(Numbers<TNum>.Fourth);
    public TNum Volume => Axis.Length * Base.SurfaceArea * Numbers<TNum>.Third;
    public Vec3<TNum> Tip => Axis.End;
    public Circle3<TNum> Base => new(Axis.Start, Axis.AxisVector, Radius);

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
        return new Circle3<TNum>(c, Axis.AxisVector, radius);
    }

    public TNum GetRadiusAt(TNum normHeight) => TNum.Lerp(Radius, TNum.Zero, normHeight);

    /// <inheritdoc />
    public AABB<Vec3<TNum>> BBox => Base.BBox.CombineWith(Tip);

    /// <inheritdoc />
    public IMesh<TNum> Tessellate() => Tessellate(32);

    public IndexedMesh<TNum> Tessellate(int edgeCount)
    {
        var baseMesh = Base.Reversed().Tessellate(edgeCount).Indexed();
        var indices = new TriangleIndexer[edgeCount * 2];
        baseMesh.Indices.CopyTo(indices, 0);
        Vec3<TNum>[] vertices = [..baseMesh.Vertices, Tip];
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
    public IDiscreteCurve<Vec3<TNum>, TNum> SweepCurve => Base.Traverse(TNum.Zero).LineTo(Tip);

    /// <inheritdoc />
    public Ray3<TNum> SweepAxis => Axis.Start.RayThrough(Axis.End);

    /// <inheritdoc />
    public Vec3<TNum> NormalAt(Vec3<TNum> p)
    {
        p = ClampToSurface(p);
        return NormalAtUnsafe(p);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3<TNum> NormalAtUnsafe(Vec3<TNum> p)
    {
        var baseC = Base;
        if (p.IsApprox(Tip))
            return Axis.Direction;
        var p2 = baseC.Plane.ProjectIntoLocal(p - baseC.Centroid);
        if (p2.IsApproxZero()) return Axis.Direction;
        var anglePos = p2.CartesianToPolar().PolarAngle;
        var tangent = baseC.GetTangentAtAngle(anglePos);
        var pToTip = Tip - p;
        return tangent.Cross(pToTip).Normalized();
    }
    


    public TNum ApexAngle => TNum.Atan(Radius / Axis.Length);

    [Pure]
    public Vec3<TNum> ClampToSurface(Vec3<TNum> p)
    {
        var (closest, onSeg) = Axis.ClosestPoints(p);
        if (onSeg.IsApprox(Tip))
            return Tip;
        p += onSeg - closest;
        var pos = onSeg.DistanceTo(Axis.Start) / Axis.Length;
        var radius = TNum.Abs(RadiusAt(pos));
        return Vec3<TNum>.ExactLerp(onSeg, p, radius);
    }

    public TNum RadiusAt(TNum pos) => TNum.Lerp(Radius, TNum.Zero, pos);


    [Pure]
    public Vec2<TNum> ProjectPoint(Vec3<TNum> p)
    {
        p = ClampToSurface(p);
        if (p.IsApprox(Tip)) return Vec2<TNum>.Zero;
        var baseCircle = Base;
        var basis = baseCircle.Basis;
        var baseToP = p - Axis.Start;
        var angle = Vec3<TNum>.SignedAngleBetween(basis.U, baseToP, baseCircle.Normal);
        if (TNum.IsNaN(angle)) return Vec2<TNum>.Zero; //no angle means at tip but not detected via is Approx
        var relativeAngleSpace = baseCircle.Radius / SlantHeight;
        var realAngle = angle * relativeAngleSpace;
        var polarRadius = p.DistanceTo(Tip);
        Vec2<TNum> polar = new(polarRadius, realAngle);
        return polar.PolarToCartesian();
    }

    [Pure]
    public Vec3<TNum> ProjectPoint(Vec2<TNum> p)
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
        return Vec3<TNum>.Lerp(coneTip, basePt, tipToBase);
    }

    [Pure]
    public Ray2<TNum> ProjectDirection(Vec3<TNum> p, Vec3<TNum> direction)
    {
        var origin = ClampToSurface(p);
        var onSurface = ProjectPoint(p);
        var normal = NormalAt(p);
        var tipToOrigin = origin - Tip;
        var angle = Vec3<TNum>.SignedAngleBetween(tipToOrigin, direction, normal);
        var polarOnSurface = onSurface.CartesianToPolar();
        Vec2<TNum> projectedDir = new(TNum.One, polarOnSurface.PolarAngle + angle);
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

    public static bool operator ==(Cone<TNum> left, Cone<TNum> right) => left.Equals(right);
    public static bool operator !=(Cone<TNum> left, Cone<TNum> right) => !left.Equals(right);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Axis, Radius);
    }

    /// <inheritdoc />
    public ConeGeodesic<TNum> GetGeodesic(Vec3<TNum> p1, Vec3<TNum> p2)
        => ConeGeodesic<TNum>.BetweenPoints(in this, p1, p2);

    /// <inheritdoc />
    public ConeGeodesic<TNum> GetGeodesicFromEntry(Vec3<TNum> entryPoint, Vec3<TNum> direction)
        => ConeGeodesic<TNum>.FromDirection(in this, entryPoint, direction);
}