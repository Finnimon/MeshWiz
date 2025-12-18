using System.Diagnostics.Contracts;
using System.Numerics;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public readonly struct Circle3<TNum> : IFlat<TNum>, IContiguousDiscreteCurve<Vector3<TNum>, TNum>,
    IRotationalSurface<TNum>, IGeodesicProvider<PoseLine<Pose3<TNum>, Vector3<TNum>, TNum>, TNum>,
    IEquatable<Circle3<TNum>>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    /// <inheritdoc />
    public Vector3<TNum> Start => TraverseByAngle(TNum.Zero);

    /// <inheritdoc />
    public Vector3<TNum> End => Start;

    /// <inheritdoc />
    public Vector3<TNum> TraverseOnCurve(TNum t)
        => Traverse(t);

    /// <inheritdoc />
    public TNum Length => Circumference;

    /// <inheritdoc />
    public bool IsClosed => true;

    /// <inheritdoc />
    public Polyline<Vector3<TNum>, TNum> ToPolyline()
    {
        const int edgeCount = 32;
        var verts = new Vector3<TNum>[edgeCount + 1];
        var edgeCountNum = TNum.CreateTruncating(edgeCount);
        for (var i = 0; i < verts.Length - 1; i++)
        {
            var angle = TNum.CreateTruncating(i) / edgeCountNum * Numbers<TNum>.TwoPi;
            verts[i] = TraverseByAngle(angle);
        }

        verts[^1] = verts[0];
        return new(verts);
    }

    /// <inheritdoc />
    public Polyline<Vector3<TNum>, TNum> ToPolyline(PolylineTessellationParameter<TNum> tessellationParameter)
    {
        var steps = Numbers<TNum>.TwoPi / tessellationParameter.MaxAngularDeviation;
        steps = TNum.Round(steps, MidpointRounding.AwayFromZero);
        var intSteps = int.CreateTruncating(steps);
        intSteps = int.Abs(intSteps);
        var verts = new Vector3<TNum>[intSteps + 1];
        var angleStep = Numbers<TNum>.TwoPi / TNum.CreateTruncating(intSteps);
        var i = -1;
        for (var angle = TNum.Zero; angle < Numbers<TNum>.TwoPi; angle += angleStep)
            verts[++i] = this.TraverseByAngle(angle);

        verts[^1] = verts[0];
        return new(verts);
    }

    public TNum Circumference => Numbers<TNum>.TwoPi * Radius;
    public readonly Vector3<TNum> Center;
    public Vector3<TNum> Centroid => Center;
    public readonly Vector3<TNum> N;
    public Vector3<TNum> Normal => N;
    public readonly TNum Radius;

    /// <param name="centroid"></param>
    /// <param name="normal"></param>
    /// <param name="radius">negative radius reverses <paramref name="normal"/></param>
    public Circle3(Vector3<TNum> centroid, Vector3<TNum> normal, TNum radius)
    {
        Center = centroid;
        var absRadius = TNum.Abs(radius);
        var sign = radius / absRadius;
        N = normal.Normalized() * sign;
        Radius = absRadius;
    }

    public TNum Diameter => Radius * Numbers<TNum>.Two;
    public Plane3<TNum> Plane => new(N, Center);
    public TNum SurfaceArea => Radius * Radius * TNum.Pi;
    public (Vector3<TNum> U, Vector3<TNum> V) Basis => Plane.Basis;


    /// <inheritdoc />
    public AABB<Vector3<TNum>> BBox
    {
        get
        {
            var (u, v) = Basis;
            var diag = Vector3<TNum>.Abs(u) + Vector3<TNum>.Abs(v);
            diag *= Diameter;
            return AABB.Around(Center, diag);
        }
    }


    public IMesh<TNum> Tessellate() => Tessellate(32);

    public IIndexedMesh<TNum> Tessellate(int edgeCount)
        => new InscribedPolygon3<TNum>(edgeCount, this).Tessellate().Indexed();

    public Vector3<TNum> Traverse(TNum t)
        => TraverseByAngle(t * Numbers<TNum>.TwoPi);

    public Vector3<TNum> TraverseByAngle(TNum angle)
    {
        var (u, v) = Plane.Basis; // assumed normalized orthonormal basis
        var (sin, cos) = TNum.SinCos(angle);
        return Center + u * (cos * Radius) + v * (sin * Radius);
    }

    public Circle3<TNum> Reversed()
        => new(Center, -N, Radius);

    /// <inheritdoc />
    public IDiscreteCurve<Vector3<TNum>, TNum> SweepCurve => (Basis.U * Radius + Center).LineTo(Center);

    /// <inheritdoc />
    public Ray3<TNum> SweepAxis => new(Center, Normal);

    public Circle3Section<TNum> Cutout(TNum start, TNum end)
    {
        var minor = start * Radius;
        var major = end * Radius;
        return new Circle3Section<TNum>(Center, N, minor, major);
    }

    public Arc3<TNum> Section(TNum start, TNum end)
    {
        var startAngle = start * Numbers<TNum>.TwoPi;
        var endAngle = end * Numbers<TNum>.TwoPi;
        return ArcSection(startAngle, endAngle);
    }

    public Arc3<TNum> ArcSection(TNum startAngle, TNum endAngle) => new(this, startAngle, endAngle);

    public Vector3<TNum> GetTangentAtAngle(TNum angle)
    {
        var (u, v) = Plane.Basis; // assumed orthonormal basis
        var cos = TNum.Cos(angle);
        var sin = TNum.Sin(angle);

        var tangent = (-u * sin + v * cos) * Radius;

        return tangent.Normalized();
    }


    /// <inheritdoc />
    public Vector3<TNum> GetTangent(TNum t)
        => GetTangentAtAngle(t * Numbers<TNum>.TwoPi);

    /// <inheritdoc />
    public Vector3<TNum> EntryDirection => GetTangentAtAngle(TNum.Zero);

    /// <inheritdoc />
    public Vector3<TNum> ExitDirection => EntryDirection;

    public Circle2<TNum> OnPlane => new(Plane.ProjectIntoLocal(Center), TNum.Abs(Radius));

    /// <inheritdoc />
    public Vector3<TNum> NormalAt(Vector3<TNum> _)
        => Normal;

    /// <inheritdoc />
    public Vector3<TNum> ClampToSurface(Vector3<TNum> p)
    {
        p = Plane.Clamp(p);
        var cToP = p - Center;
        var len = cToP.Length;
        cToP = cToP.Normalized();
        var adjust = TNum.Clamp(len, TNum.Zero, TNum.One);
        return adjust * cToP + Center;
    }

    [Pure]
    public Vector3<TNum> ClampToEdge(Vector3<TNum> p)
    {
        p = Plane.Clamp(p);
        var cToP = p - Center;
        var len = cToP.Length;
        var adjust = Radius / len;
        return adjust * cToP + Center;
    }

    /// <inheritdoc />
    public PoseLine<Pose3<TNum>, Vector3<TNum>, TNum> GetGeodesic(Vector3<TNum> p1, Vector3<TNum> p2)
    {
        p1 = ClampToSurface(p1);
        p2 = ClampToSurface(p2);
        return PoseLine<Pose3<TNum>, Vector3<TNum>, TNum>.FromLine(p1, p2, Normal);
    }

    /// <inheritdoc />
    public PoseLine<Pose3<TNum>, Vector3<TNum>, TNum> GetGeodesicFromEntry(Vector3<TNum> entryPoint,
        Vector3<TNum> direction)
    {
        entryPoint = ClampToSurface(entryPoint);
        direction -= N * direction.Dot(Normal);
        var dirLen = direction.Length;
        if (!TNum.IsFinite(dirLen) || dirLen.IsApproxZero())
            ThrowHelper.ThrowInvalidOperationException();

        var hit = RayCircleIntersect(entryPoint, direction, Center, Radius, out var t);
        if (!hit)
            ThrowHelper.ThrowInvalidOperationException();
        var exitPoint = direction * t + entryPoint;
        return PoseLine<Pose3<TNum>, Vector3<TNum>, TNum>.FromLine(entryPoint, exitPoint, Normal);
    }

    private static bool RayCircleIntersect(
        Vector3<TNum> rayOrigin,
        Vector3<TNum> rayDir, // normalized not required
        Vector3<TNum> circleCenter,
        TNum circleRadius,
        out TNum t)
    {
        t = TNum.NaN;
        var oc = circleCenter - rayOrigin;
        // Projection of center onto direction
        var tca = oc.Dot(rayDir);

        var ocLen2 = oc.SquaredLength;
        var d2 = ocLen2 - tca * tca;

        var r2 = circleRadius * circleRadius;

        if (d2 > r2)
        {
            return false;
        }

        var thc = TNum.Sqrt(r2 - d2);
        t = tca + thc;

        return t > TNum.Zero;
    }

    /// <inheritdoc />
    public bool Equals(Circle3<TNum> other)
    {
        return Radius.Equals(other.Radius) && Center.Equals(other.Center) && Normal.Equals(other.Normal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Circle3<TNum> other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Radius, Center, Normal);
    }

    public static bool operator ==(Circle3<TNum> left, Circle3<TNum> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Circle3<TNum> left, Circle3<TNum> right)
    {
        return !left.Equals(right);
    }
}