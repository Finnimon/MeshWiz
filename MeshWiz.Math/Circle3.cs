using System.Numerics;

namespace MeshWiz.Math;

public readonly struct Circle3<TNum> : IFlat<TNum>, ISurface3<TNum>, ICurve<Vector3<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    /// <inheritdoc />
    public bool IsClosed => true;

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
}