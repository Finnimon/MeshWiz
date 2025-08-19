using System.Numerics;
using MeshWiz.Math;

namespace MeshWiz.Slicer;

public static class Slicing
{
    public static IndexedMesh<TNum> GrowMesh<TNum>(IIndexedMesh<TNum> source, TNum growBy)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var vertices = source.Vertices;
        var indices = source.Indices;
        var normals = new Vector3<TNum>[vertices.Length];
        var normalCount = new TNum[vertices.Length];
        foreach (var indexer in indices)
        {
            var normal = indexer.Extract(vertices).Normal;
            var (a, b, c) = indexer;
            normals[a] += normal;
            normals[b] += normal;
            normals[c] += normal;
            normalCount[a]++;
            normalCount[b]++;
            normalCount[c]++;
        }

        var offsetVertices = new Vector3<TNum>[vertices.Length];
        for (var i = 0; i < normalCount.Length; i++)
        {
            var normal = normals[i];
            normal = (normal / normalCount[i]).Normalized;
            offsetVertices[i] = normal * growBy + vertices[i];
        }

        return new IndexedMesh<TNum>(offsetVertices, indices);
    }
}