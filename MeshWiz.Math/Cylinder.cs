using System.Numerics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public readonly struct Cylinder<TNum> : IBody<TNum>, IRotationalSurface<TNum>, IGeodesicProvider<Helix<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly TNum Radius;
    public readonly Line<Vector3<TNum>, TNum> Axis;
    public TNum Height => Axis.Length;
    public Circle3<TNum> Top => new(Axis.End, Axis.Direction, Radius);
    public Circle3<TNum> Base => new(Axis.Start, Axis.Direction, Radius);
    public TNum SurfaceArea => Top.Circumference * Axis.Length + Top.SurfaceArea * Numbers<TNum>.Two;
    public TNum Volume => Top.SurfaceArea * Axis.Length;
    public Vector3<TNum> Centroid => Axis.MidPoint;
    public AABB<Vector3<TNum>> BBox => Base.BBox.CombineWith(Top.BBox);

    public Cylinder(Line<Vector3<TNum>, TNum> axis, TNum radius)
    {
        if (TNum.IsPositive(radius))
        {
            Radius = radius;
            Axis = axis;
            return;
        }

        Radius = -radius;
        Axis = axis.Reversed();
    }

    public IMesh<TNum> Tessellate() => ConeSection<TNum>.TessellateCylindrical(Base, Top, 32);

    /// <inheritdoc />
    public IDiscreteCurve<Vector3<TNum>, TNum> SweepCurve
    {
        get
        {
            var start = Base.TraverseByAngle(TNum.Zero);
            var end = start + Axis.Direction;
            return start.LineTo(end);
        }
    }

    /// <inheritdoc />
    public Ray3<TNum> SweepAxis => Axis.Start.RayThrough(Axis.End);

    public TNum Circumference => Radius * Numbers<TNum>.TwoPi;

    public Vector3<TNum> ClampToSurface(Vector3<TNum> p)
    {
        var closestOnAxis = Axis.ClosestPoint(p);
        var d = closestOnAxis.DistanceTo(p);
        var radialDiff = Radius / d;
        if (!radialDiff.IsApprox(TNum.One, Numbers<TNum>.Eps4))
            p = Vector3<TNum>.Lerp(closestOnAxis, p, radialDiff);
        var vPos = closestOnAxis.DistanceTo(Axis.Start);
        var downWards = TNum.IsNegative((closestOnAxis - Axis.Start).Dot(Axis.Direction));
        if (downWards) vPos = -vPos;
        var newVPos = AABB.From(TNum.Zero, Height).Clamp(vPos);
        if (newVPos == vPos) return p;
        var vShift = newVPos - vPos;
        return p + Axis.NormalDirection * vShift;
    }

    /// <inheritdoc />
    public Helix<TNum> GetGeodesic(Vector3<TNum> p1, Vector3<TNum> p2) 
        => Helix<TNum>.BetweenPoints(in this, p1, p2);

    /// <inheritdoc />
    public Helix<TNum> GetGeodesicFromEntry(Vector3<TNum> entryPoint, Vector3<TNum> direction)
        => Helix<TNum>.FromOrigin(in this, entryPoint, direction);
}