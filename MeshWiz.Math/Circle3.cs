using System.Diagnostics;
using System.Numerics;

namespace MeshWiz.Math;

public readonly struct Circle3<TNum> : IFlat<TNum>, ISurface3<TNum>, IDiscreteCurve<Vector3<TNum>, TNum>,
    IRotationalSurface<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    /// <inheritdoc />
    public Vector3<TNum> Start => TraverseByAngle(TNum.Zero);

    /// <inheritdoc />
    public Vector3<TNum> End => Start;

    /// <inheritdoc />
    public Vector3<TNum> TraverseOnCurve(TNum distance)
        => Traverse(distance);

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
        {
            verts[++i] = this.TraverseByAngle(angle);
        }

        verts[^1] = verts[0];
        return new(verts);
    }

    public TNum Circumference => Numbers<TNum>.TwoPi;
    public Vector3<TNum> Centroid { get; }
    public Vector3<TNum> Normal { get; }
    public readonly TNum Radius;

    /// <param name="centroid"></param>
    /// <param name="normal"></param>
    /// <param name="radius">negative radius reverses <paramref name="normal"/></param>
    public Circle3(Vector3<TNum> centroid, Vector3<TNum> normal, TNum radius)
    {
        Centroid = centroid;
        var absRadius = TNum.Abs(radius);
        var sign = radius / absRadius;
        Normal = normal.Normalized * sign;
        Radius = absRadius;
    }

    public TNum Diameter => Radius * Numbers<TNum>.Two;
    public Plane3<TNum> Plane => new(Normal, Centroid);
    public TNum SurfaceArea => Radius * Radius * TNum.Pi;
    public (Vector3<TNum> U, Vector3<TNum> V) Uv => Plane.Basis;


    /// <inheritdoc />
    public AABB<Vector3<TNum>> BBox
    {
        get
        {
            var (u, v) = Plane.Basis;
            var diag = Vector3<TNum>.Abs(u) + Vector3<TNum>.Abs(v);
            diag *= Diameter;
            return AABB.Around(Centroid, diag);
        }
    }


    public IMesh<TNum> Tessellate() => Tessellate(32);

    public IIndexedMesh<TNum> Tessellate(int edgeCount)
        => new InscribedPolygon3<TNum>(edgeCount, this).Tessellate().Indexed();

    public Vector3<TNum> Traverse(TNum distance)
        => TraverseByAngle(distance * Numbers<TNum>.TwoPi);

    public Vector3<TNum> TraverseByAngle(TNum angle)
    {
        var (u, v) = Plane.Basis; // assumed normalized orthonormal basis

        var cos = TNum.Cos(angle);
        var sin = TNum.Sin(angle);

        return Centroid + u * (cos * Radius) + v * (sin * Radius);
    }

    public Circle3<TNum> Reversed()
        => new(Centroid, -Normal, Radius);

    /// <inheritdoc />
    public IDiscreteCurve<Vector3<TNum>, TNum> SweepCurve => (Uv.U * Radius + Centroid).LineTo(Centroid);

    /// <inheritdoc />
    public Ray3<TNum> SweepAxis => new(Centroid, Normal);

    public Circle3Section<TNum> Cutout(TNum start, TNum end)
    {
        var minor = start * Radius;
        var major = end * Radius;
        return new Circle3Section<TNum>(Centroid, Normal, minor, major);
    }

    public Arc3<TNum> Section(TNum start, TNum end)
    {
        var startAngle=start*Numbers<TNum>.TwoPi;
        var endAngle=end*Numbers<TNum>.TwoPi;
        return ArcSection(startAngle, endAngle);
    }

    public Arc3<TNum> ArcSection(TNum startAngle, TNum endAngle) => new(this,startAngle,endAngle);
}