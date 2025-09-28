using System.Numerics;

namespace MeshWiz.Math;

public readonly struct InscribedPolygon3<TNum>(int edgeCount, Circle3<TNum> boundary)
    : IFlat<TNum>, ISurface3<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly int EdgeCount =
        edgeCount > 2 ? edgeCount : throw new ArgumentOutOfRangeException(nameof(edgeCount));

    public readonly Circle3<TNum> Boundary = boundary;
    public TNum StepAngle => Numbers<TNum>.TwoPi / TNum.CreateTruncating(edgeCount);

    /// <inheritdoc />
    public Vector3<TNum> Normal => Boundary.Normal;

    /// <inheritdoc />
    public Plane3<TNum> Plane => Boundary.Plane;

    /// <inheritdoc />
    public Vector3<TNum> Centroid => Boundary.Centroid;

    /// <inheritdoc />
    /// faster than explicit check of every Vertex
    public AABB<Vector3<TNum>> BBox => Boundary.BBox;

    /// <inheritdoc />
    public IMesh<TNum> Tessellate()
    {
        var vertices = new Vector3<TNum>[EdgeCount + 1];
        vertices[0] = Centroid;
        var outerBounds = vertices.AsSpan(1);
        var pCount = TNum.CreateTruncating(EdgeCount);
        var step = StepAngle;
        var i = -1;
        for (var angle = TNum.Zero; angle < pCount * step; angle += step)
            outerBounds[++i] = Boundary.TraverseByAngle(angle);
        var indices = new TriangleIndexer[EdgeCount];
        for (i = 0; i < EdgeCount - 1; i++) indices[i] = new TriangleIndexer(0, i + 1, i + 2);
        indices[^1] = new TriangleIndexer(0, EdgeCount, 1);
        return new IndexedMesh<TNum>(vertices, indices);
    }

    /// <inheritdoc />
    public TNum SurfaceArea
    {
        get
        {
            var c = Boundary.Centroid;
            var p1 = Boundary.TraverseByAngle(TNum.Zero);
            var p2 = Boundary.TraverseByAngle(StepAngle);
            return new Triangle3<TNum>(c, p1, p2).SurfaceArea * TNum.CreateTruncating(EdgeCount);
        }
    }
}