using System.Numerics;

namespace MeshWiz.Math;

public static partial class Mesh
{
    public static class Transforms
    {
        public static IndexedMesh<TNum> Scale<TNum>(IIndexedMesh<TNum> source, TNum by,Vec3<TNum>? about=null) where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var aboutPt =about?? source.VertexCentroid;
            var vertices = source.Vertices.Select(v => by * (v - aboutPt) + aboutPt).ToArray();
            var indices = source.Indices.ToArray();
            return new(vertices, indices);
        }
    }
}