using System.Diagnostics.Contracts;
using System.Numerics;
using MeshWiz.Utility;

namespace MeshWiz.Math;

public readonly struct Cone<TNum> : IBody<TNum>, IRotationalSurface<TNum>
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
    public TNum Volume => Axis.Length * Base.SurfaceArea * Numbers<TNum>.Fourth;
    public Vector3<TNum> Tip => Axis.End;
    public Circle3<TNum> Base => new(Axis.Start, Axis.Direction, Radius);

    /// <inheritdoc />
    public TNum SurfaceArea => Base.SurfaceArea + OpenConeSurfaceArea(Radius, Axis.Length);

    public static TNum OpenConeSurfaceArea(TNum radius, TNum height)
    {
        var s = TNum.Sqrt(radius * radius + height * height);
        return radius * s * TNum.Pi;
    }

    /// <returns><see cref="Base"/> projected along <see cref="Axis"/> by <paramref name="exactHeight"/></returns>
    public Circle3<TNum> GetCircleAtExact(TNum exactHeight) => GetCircleAt(exactHeight / Axis.Length);

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
        var closest = Axis.ClosestPoint(p);
        var axisToP = (p - closest).Normalized;
        var axisN = Axis.Direction;
        var axisLen = axisN.Length;
        axisN /= axisLen;
        //point==Tip we set up as normal
        if (!axisToP.IsNormalized) return axisN;
        var apexAngle = TNum.Atan(Radius / axisLen);
        var (sin, cos) = TNum.SinCos(apexAngle);
        return cos * axisN + sin * axisToP;
    }

    public TNum ApexAngle => TNum.Atan(Radius / Axis.Length);

    [Pure]
    public Vector3<TNum> ClampToSurface(Vector3<TNum> p)
    {
        var (closest, onseg) = Axis.ClosestPoints(p);
        p += onseg - closest;
        var pos = onseg.DistanceTo(Axis.Start) / Axis.Length;
        var radius = TNum.Abs(RadiusAt(pos));
        return Vector3<TNum>.ExactLerp(onseg, p, radius);
    }

    public TNum RadiusAt(TNum pos) => TNum.Lerp(Radius, TNum.Zero, pos);
}

public readonly struct ConeGeodesic<TNum> where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Cone<TNum> Surface;
    public readonly Line<Vector2<TNum>, TNum> ProjectedLine;
    public TNum AngleRangeSize => (Surface.Radius / Surface.SlantHeight) * Numbers<TNum>.TwoPi;
    public AABB<TNum> AngleRange => AABB.Around(TNum.Zero, AngleRangeSize);
    public Circle2<TNum> ProjectedCircle => new(Vector2<TNum>.Zero, Surface.SlantHeight);

    [Pure]
    public Vector3<TNum> ProjectPoint(Vector2<TNum> p)
    {
        var polarPt = p.CartesianToPolar();
        var baseAngle = polarPt.PolarAngle;
        var coneTip = Surface.Tip;
        var baseCircle= Surface.Base;
        var proj = ProjectedCircle;
        var relativeAngleSpace= proj.Radius/baseCircle.Radius;
        var realAngle = relativeAngleSpace * baseAngle;
        var tipToBase = polarPt.PolarRadius/proj.Radius;
        var basePt = baseCircle.TraverseByAngle(realAngle);
        return Vector3<TNum>.Lerp(coneTip, basePt, tipToBase);
    }

    [Pure]
    public Vector2<TNum> ProjectPoint(Vector3<TNum> p)
    {
        p = Surface.ClampToSurface(p);
        if(p.IsApprox(Surface.Tip))return Vector2<TNum>.Zero;
        var baseCircle= Surface.Base;
        var basis = baseCircle.Basis;
        var baseToP = p - baseCircle.Centroid;
        var angle = Vector3<TNum>.SignedAngleBetween(baseToP, basis.U, baseCircle.Normal);
        var projectedCircle = ProjectedCircle;
        var relativeAngleSpace= baseCircle.Radius/projectedCircle.Radius;
        var realAngle = angle * relativeAngleSpace;
        var polarRadius = p.DistanceTo(Surface.Tip);
        Vector2<TNum> polar = new(polarRadius, realAngle);
        return polar.PolarToCartesian();
    }
    
}