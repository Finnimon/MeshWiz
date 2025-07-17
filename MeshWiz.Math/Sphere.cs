using System;
using System.Linq;
using System.Numerics;

namespace MeshWiz.Math;

public readonly record struct Sphere<TNum>(Vector3<TNum> Centroid, TNum Radius)
    : IBody<TNum>, IFace<Vector3<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public TNum Volume
        => TNum.CreateChecked(4) * TNum.Pi * Radius * Radius * Radius / TNum.CreateChecked(3);

    public TNum SurfaceArea => TNum.CreateChecked(4) * TNum.Pi * Radius * Radius;

    public IFace<Vector3<TNum>, TNum> Surface => throw new NotImplementedException("Explicit Sphere face missing");
    public Triangle3<TNum>[] TessellatedSurface => GenerateTessellation(Centroid, Radius, stacks: 16, slices: 32);

    public BBox3<TNum> BBox
    {
        get
        {
            var offset = Vector3<TNum>.One * Radius;
            return new BBox3<TNum>(Centroid - offset, Centroid + offset);
        }
    }
    
    
    public static Triangle3<TNum>[] GenerateTessellation(in Sphere<TNum> sphere, int stacks, int slices)=>GenerateTessellation(sphere.Centroid, sphere.Radius, stacks, slices);

    public static Triangle3<TNum>[] GenerateTessellation(Vector3<TNum> center, TNum radius, int stacks, int slices)
    {
        // Generate vertices
        var verts = new Vector3<TNum>[(stacks + 1) * (slices + 1)];
        for (var i = 0; i <= stacks; i++)
        {
            var theta = TNum.CreateChecked(i) * TNum.Pi / TNum.CreateChecked(stacks);
            var sinTheta = TNum.Sin(theta);
            var cosTheta = TNum.Cos(theta);

            for (var j = 0; j <= slices; j++)
            {
                var phi = TNum.CreateChecked(j) * TNum.CreateChecked(2) * TNum.Pi / TNum.CreateChecked(slices);
                var sinPhi = TNum.Sin(phi);
                var cosPhi = TNum.Cos(phi);

                verts[i * (slices + 1) + j] = center + new Vector3<TNum>(
                    radius * sinTheta * cosPhi,
                    radius * cosTheta,
                    radius * sinTheta * sinPhi
                );
            }
        }

        // Precompute triangle count: two per quad
        var quadCount = stacks * slices;
        var triCount = quadCount * 2;
        var tris = new Triangle3<TNum>[triCount];
        var t = 0;

        for (var i = 0; i < stacks; i++)
        {
            for (var j = 0; j < slices; j++)
            {
                var first = i * (slices + 1) + j;
                var second = first + slices + 1;

                // Two triangles per quad with CCW winding
                tris[t++] = new Triangle3<TNum>(verts[first], verts[first + 1], verts[second]);
                tris[t++] = new Triangle3<TNum>(verts[second], verts[first + 1], verts[second + 1]);
            }
        }

        return tris;
    }

}