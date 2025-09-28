using System.Numerics;

namespace MeshWiz.Math;

public readonly struct Circle3<TNum> : IFlat<TNum>, ISurface3<TNum>, IDiscreteCurve<Vector3<TNum>, TNum>, IRotationalSurface<TNum>
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
    Polyline<Vector3<TNum>, TNum> IDiscreteCurve<Vector3<TNum>, TNum>.ToPolyline()
    {
        const int edgeCount = 32;
        var verts = new Vector3<TNum>[edgeCount+1];
        var edgeCountNum=TNum.CreateTruncating(edgeCount);
        for (var i = 0; i < verts.Length-1; i++)
        {
            var angle=TNum.CreateTruncating(i)/edgeCountNum*Numbers<TNum>.TwoPi;
            verts[i]=TraverseByAngle(angle);
        }

        verts[^1] = verts[0];
        return new(verts);
    }

    /// <inheritdoc />
    public Polyline<Vector3<TNum>, TNum> ToPolyline(PolylineTessellationParameter<TNum> tessellationParameter)
    {
        throw new NotImplementedException();
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
    public (Vector3<TNum> u, Vector3<TNum> v) Uv => Plane.Uv;


    /// <inheritdoc />
    public AABB<Vector3<TNum>> BBox
    {
        get
        {
            var (u, v) = Plane.Uv;
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
        var (u, v) = Plane.Uv; // assumed normalized orthonormal basis

        var cos = TNum.Cos(angle);
        var sin = TNum.Sin(angle);

        return Centroid + u * (cos * Radius) + v * (sin * Radius);
    }

    public Circle3<TNum> Reversed()
        => new(Centroid, -Normal, Radius);
    
    /// <inheritdoc />
    public IDiscreteCurve<Vector3<TNum>, TNum> SweepCurve => Uv.u*Radius+Centroid.LineTo(Centroid);

    /// <inheritdoc />
    public Ray3<TNum> SweepAxis => new(Centroid,Normal);
}