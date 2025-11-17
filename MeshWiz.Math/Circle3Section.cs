using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Numerics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public readonly struct Circle3Section<TNum> : IFlat<TNum>, IRotationalSurface<TNum>,
    IGeodesicProvider<Line<Vector3<TNum>, TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public Vector3<TNum> Centroid { get; }
    public Vector3<TNum> Normal { get; }
    public readonly TNum MinorRadius;
    public readonly TNum MajorRadius;
    public Circle3<TNum> Major => new(Centroid, Normal, MajorRadius);
    public Circle3<TNum> Minor => new(Centroid, Normal, MinorRadius);

    /// <param name="centroid">centroid</param>
    /// <param name="normal">circle normal</param>
    /// <param name="minorRadius">inner radius<paramref name="normal"/></param>
    /// <param name="majorRadius">outer radius</param>
    public Circle3Section(Vector3<TNum> centroid, Vector3<TNum> normal, TNum minorRadius, TNum majorRadius)
    {
        MinorRadius = TNum.Abs(minorRadius);
        MajorRadius = TNum.Abs(majorRadius);
        Centroid = centroid;
        ArgumentOutOfRangeException.ThrowIfEqual(MajorRadius, MinorRadius, nameof(MajorRadius));
        var sign = TNum.Sign(minorRadius) != TNum.Sign(majorRadius) ^ majorRadius < minorRadius;
        Normal = sign ? -normal.Normalized() : normal.Normalized();
    }

    public Plane3<TNum> Plane => new(Normal, Centroid);
    public TNum SurfaceArea => (MajorRadius * MajorRadius - MinorRadius * MinorRadius) * TNum.Pi;


    /// <inheritdoc />
    public AABB<Vector3<TNum>> BBox => Major.BBox;


    public IMesh<TNum> Tessellate() => Tessellate(32);

    public IIndexedMesh<TNum> Tessellate(int edgeCount)
        => Surface.Rotational.Tessellate<Circle3Section<TNum>, TNum>(this, edgeCount);


    /// <inheritdoc />
    public IDiscreteCurve<Vector3<TNum>, TNum> SweepCurve =>
        Minor.TraverseByAngle(TNum.Zero).LineTo(Major.TraverseByAngle(TNum.Zero));

    /// <inheritdoc />
    public Ray3<TNum> SweepAxis => new(Centroid, Normal);

    public Line<Vector3<TNum>, TNum> GetGeodesic(Vector3<TNum> p1, Vector3<TNum> p2)
    {
        var p1p = Plane.ProjectIntoWorld(Plane.ProjectIntoLocal(p1));
        var p2p = Plane.ProjectIntoWorld(Plane.ProjectIntoLocal(p2));

        static TNum RadialDistance(Vector3<TNum> c, Vector3<TNum> pt)
            => (pt - c).Length;

        var r1 = RadialDistance(Centroid, p1p);
        var r2 = RadialDistance(Centroid, p2p);
        var mid = (p1p + p2p) * TNum.CreateTruncating(0.5);
        var rm = RadialDistance(Centroid, mid);

        var minR = r1;
        if (r2 < minR) minR = r2;
        if (rm < minR) minR = rm;

        var maxR = r1;
        if (r2 > maxR) maxR = r2;
        if (rm > maxR) maxR = rm;

        var inside = minR >= MinorRadius && maxR <= MajorRadius;

        if (inside)
        {
            var dir = p2p - p1p;
            var dirLen = dir.Length;
            if (dirLen == TNum.Zero) dir = Plane.Basis.U; // degenerate -> use plane basis
            else dir /= dirLen;
            return new Line<Vector3<TNum>, TNum>(p1p, dir);
        }


        var cp1 = ClampRadial(p1p);
        var cp2 = ClampRadial(p2p);

        var fallbackDir = cp2 - cp1;
        var fallbackLen = fallbackDir.Length;
        if (fallbackLen == TNum.Zero) fallbackDir = Plane.Basis.U;
        else fallbackDir /= fallbackLen;

        return new Line<Vector3<TNum>, TNum>(cp1, fallbackDir);
    }

    private Vector3<TNum> ClampRadial(Vector3<TNum> pt)
    {
        var cToP = pt - Centroid;
        var dist = cToP.Length;
        if (dist == TNum.Zero)
        {
            // pick a default radial direction from the plane basis
            cToP = Plane.Basis.U;
            dist = cToP.Length;
        }

        var dir = cToP / dist;
        // clamp distance into [MinorRadius, MajorRadius]
        if (dist < MinorRadius) dist = MinorRadius;
        else if (dist > MajorRadius) dist = MajorRadius;

        return Centroid + dir * dist;
    }

    // /// <inheritdoc />
    // public Line<Vector3<TNum>, TNum> GetGeodesic(Vector3<TNum> p1, Vector3<TNum> p2)
    // {
    //     throw new NotImplementedException();
    // }

    /// <inheritdoc />
    public Line<Vector3<TNum>, TNum> GetGeodesicFromEntry(Vector3<TNum> entryPoint, Vector3<TNum> direction)
    {
        var planeClamped = Plane.ProjectIntoWorld(Plane.ProjectIntoLocal(entryPoint));
        var cToP = planeClamped - Centroid;
        var distance = cToP.Length;
        cToP /= distance;
        distance = AABB.From(TNum.Abs(MinorRadius), TNum.Abs(MajorRadius)).Clamp(distance);
        var surfaceClamped = Centroid + cToP * distance;

        var flatDir = direction - Normal * direction.Dot(Normal);
        flatDir = flatDir.Normalized();
        if (!flatDir.SquaredLength.IsApprox(TNum.One)) return surfaceClamped.LineTo(surfaceClamped);
        var line = Plane.ProjectIntoLocal(surfaceClamped).LineTo(Plane.ProjectIntoLocal(surfaceClamped + direction));
        var tMin = GetClosestIntersection(in line, Minor.OnPlane);
        var tMaj = GetClosestIntersection(in line, Major.OnPlane);
        var t = TNum.Min(tMin, tMaj);
        Debug.Assert(TNum.IsFinite(t));
        line = line.Section(TNum.Zero, t);
        return Plane.ProjectIntoWorld(line);
    }

    private static TNum GetClosestIntersection(in Line<Vector2<TNum>, TNum> l, Circle2<TNum> circle)
    {
        var t = TNum.PositiveInfinity;
        var d = l.Direction;
        var f = l.Start - circle.Center;

        var a = d.Dot(d);
        var b = Numbers<TNum>.Two * f.Dot(d);
        var c = f.Dot(f) - circle.Radius * circle.Radius;

        var disc = b * b - Numbers<TNum>.Four * a * c;
        var sign = disc.EpsilonTruncatingSign();
        if (sign == -1) return t;

        var sqrtDisc = TNum.Sqrt(disc);
        t = (-b - sqrtDisc) / (a * Numbers<TNum>.Two);
        var tValid = t.EpsilonTruncatingSign() == 1;
        if (!tValid) t = TNum.PositiveInfinity;
        if (sign == 0) return t;
        var t2 = (-b + sqrtDisc) / (a * Numbers<TNum>.Two);

        if (t2.EpsilonTruncatingSign() != 1) return t;
        if (!tValid) return t2;

        return TNum.Min(t, t2);
    }

    [Pure]
    public Vector3<TNum> NormalAt(Vector3<TNum> _) => Normal;

    /// <inheritdoc />
    public Vector3<TNum> ClampToSurface(Vector3<TNum> p)
    {
        p = Plane.Clamp(p);
        var cToP = p - Centroid;
        if (cToP.IsApproxZero())
            return Plane.Basis.U * MinorRadius;
        var len = cToP.Length;
        var proper = AABB.From(MinorRadius, MajorRadius).Clamp(len);
        var adjust = proper / len;
        return adjust * cToP + Centroid;
    }
}