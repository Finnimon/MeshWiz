using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.InteropServices;
using MeshWiz.Utility;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Cylinder<TNum> : IBody<TNum>, IRotationalSurface<TNum>, IGeodesicProvider<Helix<TNum>, TNum>, IEquatable<Cylinder<TNum>>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Line<Vec3<TNum>, TNum> Axis;
    public readonly TNum Radius;
    public TNum Height => Axis.Length;
    public Circle3<TNum> Top => new(Axis.End, Axis.AxisVector, Radius);
    public Circle3<TNum> Base => new(Axis.Start, Axis.AxisVector, Radius);
    public TNum SurfaceArea => Top.Circumference * Axis.Length + Top.SurfaceArea * Numbers<TNum>.Two;
    public TNum Volume => Top.SurfaceArea * Axis.Length;
    public Vec3<TNum> Centroid => Axis.MidPoint;
    public AABB<Vec3<TNum>> BBox => Base.BBox.CombineWith(Top.BBox);

    public Cylinder(Line<Vec3<TNum>, TNum> axis, TNum radius)
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
    public IDiscreteCurve<Vec3<TNum>, TNum> SweepCurve
    {
        get
        {
            var start = Base.TraverseByAngle(TNum.Zero);
            var end = start + Axis.AxisVector;
            return start.LineTo(end);
        }
    }

    /// <inheritdoc />
    public Ray3<TNum> SweepAxis => Axis.Start.RayThrough(Axis.End);

    public TNum Circumference => Radius * Numbers<TNum>.TwoPi;

    public Vec3<TNum> ClampToSurface(Vec3<TNum> p)
    {
        var (closestOnAxis, onSeg) = Axis.ClosestPoints(p);
        var d = closestOnAxis.DistanceTo(p);
        var radialDiff = Radius / d;
        p = Vec3<TNum>.Lerp(closestOnAxis, p, radialDiff);
        var vShift = onSeg - closestOnAxis;
        return p + vShift;
    }

    /// <inheritdoc />
    public Helix<TNum> GetGeodesic(Vec3<TNum> p1, Vec3<TNum> p2) 
        => Helix<TNum>.BetweenPoints(in this, p1, p2);

    /// <inheritdoc />
    public Helix<TNum> GetGeodesicFromEntry(Vec3<TNum> entryPoint, Vec3<TNum> direction)
        => Helix<TNum>.FromOrigin(in this, entryPoint, direction);

    [Pure]
    public Vec3<TNum> NormalAt(Vec3<TNum> p)
    {
        var cp= Axis.ClosestPoint(p);
        return (p - cp).Normalized();
    }

    /// <inheritdoc />
    public bool Equals(Cylinder<TNum> other)
    {
        return Radius.Equals(other.Radius) && Axis.Equals(other.Axis);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Cylinder<TNum> other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Radius, Axis);
    }

    public static bool operator ==(Cylinder<TNum> left, Cylinder<TNum> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Cylinder<TNum> left, Cylinder<TNum> right)
    {
        return !left.Equals(right);
    }
}