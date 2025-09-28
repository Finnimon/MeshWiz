using System.Numerics;

namespace MeshWiz.Math;

public readonly record struct Sphere<TNum>(Vector3<TNum> Centroid, TNum Radius)
    : IBody<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public TNum Volume
        => TNum.CreateChecked(4) * TNum.Pi * Radius * Radius * Radius / TNum.CreateChecked(3);
    public TNum Diameter=>Numbers<TNum>.Two*Radius;
    public TNum SurfaceArea => TNum.CreateChecked(4) * TNum.Pi * Radius * Radius;

    public ISurface<Vector3<TNum>, TNum> Surface => throw new NotImplementedException("Explicit Sphere face missing");

    public AABB<Vector3<TNum>> BBox => AABB.Around(Centroid, Numbers<Vector3<TNum>>.Two * Radius);
    public IMesh<TNum> Tessellate() => Tessellate(16, 32);

    public IndexedMesh<TNum> Tessellate(int stacks, int slices)
    {
        if (stacks < 2) throw new ArgumentOutOfRangeException(nameof(stacks));
        if (slices < 3) throw new ArgumentOutOfRangeException(nameof(slices));

        // Vertices
        var vertices = new Vector3<TNum>[(stacks + 1) * (slices + 1)];
        for (var i = 0; i <= stacks; i++)
        {
            var theta = TNum.CreateChecked(i) * TNum.Pi / TNum.CreateChecked(stacks);
            var sinTheta = TNum.Sin(theta);
            var cosTheta = TNum.Cos(theta);

            for (var j = 0; j <= slices; j++)
            {
                var phi = TNum.CreateChecked(j) * Numbers<TNum>.TwoPi / TNum.CreateChecked(slices);
                var sinPhi = TNum.Sin(phi);
                var cosPhi = TNum.Cos(phi);

                vertices[i * (slices + 1) + j] = Centroid + new Vector3<TNum>(
                    Radius * sinTheta * cosPhi,
                    Radius * cosTheta,
                    Radius * sinTheta * sinPhi
                );
            }
        }

        // Indices (two triangles per quad)
        var indices = new TriangleIndexer[stacks * slices * 2];
        var t = 0;
        for (var i = 0; i < stacks; i++)
        {
            for (var j = 0; j < slices; j++)
            {
                var first = i * (slices + 1) + j;
                var second = first + slices + 1;

                indices[t++] = new TriangleIndexer(first, first + 1, second);
                indices[t++] = new TriangleIndexer(second, first + 1, second + 1);
            }
        }

        return new IndexedMesh<TNum>(vertices, indices);
    }
    
    public static Triangle3<TNum>[] GenerateTessellation(Vector3<TNum> center, TNum radius, int stacks, int slices) => new Sphere<TNum>(center, radius).Tessellate(stacks, slices).ToArray();
}