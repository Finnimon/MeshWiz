using System.Diagnostics.Contracts;
using System.Numerics;
using MeshWiz.Utility;

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

    public IMesh<TNum> Tessellate() => Tessellate(32);
    public IndexedMesh<TNum> Tessellate(int edgeCount)=>ConeSection<TNum>.TessellateCylindrical(Base, Top, edgeCount);

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
        var (closestOnAxis, onSeg) = Axis.ClosestPoints(p);
        var d = closestOnAxis.DistanceTo(p);
        var radialDiff = Radius / d;
        p = Vector3<TNum>.Lerp(closestOnAxis, p, radialDiff);
        var vShift = onSeg - closestOnAxis;
        return p + vShift;
    }

    /// <inheritdoc />
    public Helix<TNum> GetGeodesic(Vector3<TNum> p1, Vector3<TNum> p2) 
        => Helix<TNum>.BetweenPoints(in this, p1, p2);

    /// <inheritdoc />
    public Helix<TNum> GetGeodesicFromEntry(Vector3<TNum> entryPoint, Vector3<TNum> direction)
        => Helix<TNum>.FromOrigin(in this, entryPoint, direction);

    [Pure]
    public Vector3<TNum> NormalAt(Vector3<TNum> p)
    {
        var cp= Axis.ClosestPoint(p);
        return (p - cp).Normalized();
    }

}