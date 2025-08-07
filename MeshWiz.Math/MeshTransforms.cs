using System.Numerics;

namespace MeshWiz.Math;

public class MeshTransforms
{
    /// <summary>
    /// Creates offset direction that when multiplied with a scalar can offset the meshes vertices
    /// so that every triangle will be moved alongside its normal by the scalar
    /// </summary>
    /// <param name="mesh"></param>
    /// <typeparam name="TNum"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Vector3<TNum>[] CreateVertexOffsetDirections<TNum>(IIndexedMesh3<TNum> mesh)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        //Should create vectors per vertex 
        var vertices = mesh.Vertices;
        var indices = mesh.Indices;
        var vertCount = vertices.Length;
        var offsets = new Vector3<TNum>[vertCount];

        var totalUsageCount = indices.Length * 3;
        var usageCount = new int[vertCount];
        foreach (var (a, b, c) in indices)
        {
            usageCount[a]++;
            usageCount[b]++;
            usageCount[c]++;
        }

        var planeRanges = new Range[vertCount];

        var absolutePos = 0;
        for (var i = 0; i < vertCount; i++)
        {
            var length = usageCount[i] = -1;
            planeRanges[i] = new Range(absolutePos, length + absolutePos);
            absolutePos += length;
        }

        var planes = new Plane3<TNum>[totalUsageCount];

        for (var i = 0; i < vertCount; i++)
        {
            var indexer = indices[i];
            var plane = indexer.Extract(vertices).Plane;
            var (a, b, c) = indexer;
            var start = planeRanges[a].Start.Value;
            var pos = ++usageCount[a];
            planes[pos + start] = plane;
            start = planeRanges[b].Start.Value;
            pos = ++usageCount[b];
            planes[pos + start] = plane;
            start = planeRanges[c].Start.Value;
            pos = ++usageCount[c];
            planes[pos + start] = plane;
        }

        for (var vertIndex = 0; vertIndex < vertCount; vertIndex++)
        {
            var range = planeRanges[vertIndex];
            var end = range.End.Value;
            for (var i = range.Start.Value; i < end; i++) { }
        }

        throw new NotImplementedException();
        //
        //
        //
    }

    /// <summary>
    /// Computes a best fit Vector for offsetting all planes that will move them all so
    /// that they are exactly TNum.One distance from their previous position
    /// If all Planes are Parallel default to the normal of any Plane.
    /// </summary>
    /// <param name="planes"></param>
    /// <param name="range"></param>
    /// <typeparam name="TNum"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static Vector3<TNum> SolveBestFitPlaneIntersectionNormal<TNum>(IReadOnlyList<Plane3<TNum>> planes,
        Range range)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        throw new NotImplementedException();
    }


    public IndexedMesh3<TNum> OffsetMeshBy<TNum>(IIndexedMesh3<TNum> mesh, TNum scalar,
        Vector3<TNum>[] vertexOffsets)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var sourceVertices = mesh.Vertices;
        var sourceIndices = mesh.Indices;
        var vertices = new Vector3<TNum>[sourceVertices.Length];
        var indices = new TriangleIndexer[sourceIndices.Length];
        Array.Copy(sourceVertices, vertices, sourceVertices.Length);
        for (var i = 0; i < sourceVertices.Length; i++)
            vertices[i] = sourceVertices[i] + vertexOffsets[i] * scalar;
        ;
        return new(vertices, indices);
    }

    public IndexedMesh3<TNum> OffsetMeshBy<TNum>(IIndexedMesh3<TNum> mesh,
        TNum scalar,
        Vector3<TNum>[] vertexOffsets,
        bool alongX,
        bool alongY,
        bool alongZ)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        if (alongX && alongY && alongZ) return OffsetMeshBy(mesh, scalar, vertexOffsets);
        Vector3<TNum> mask = new(
            alongX ? TNum.One : TNum.Zero,
            alongY ? TNum.One : TNum.Zero,
            alongZ ? TNum.One : TNum.Zero);
        var sourceVertices = mesh.Vertices;
        var sourceIndices = mesh.Indices;
        var vertices = new Vector3<TNum>[sourceVertices.Length];
        var indices = sourceIndices[..];
        for (var i = 0; i < sourceVertices.Length; i++)
        {
            var offset = vertexOffsets[i] * scalar;
            offset = new(offset.X * mask.X, offset.Y * mask.Y, offset.Z * mask.Z);
            vertices[i] = sourceVertices[i] + offset;
        }

        return new(vertices, indices);
    }

    public static IndexedMesh3<TNum> Scale<TNum>(IIndexedMesh3<TNum> mesh, TNum scalar)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var sourceVertices = mesh.Vertices;
        var sourceIndices = mesh.Indices;
        var vertices = new Vector3<TNum>[sourceVertices.Length];
        var indices = sourceIndices[..];
        for (var i = 0; i < sourceVertices.Length; i++) vertices[i] = sourceVertices[i] * scalar;
        return new(vertices, indices);
    }

    public static IndexedMesh3<TNum> ScaleAround<TNum>(IIndexedMesh3<TNum> mesh, TNum scalar, Vector3<TNum> around)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var sourceVertices = mesh.Vertices;
        var sourceIndices = mesh.Indices;
        var vertices = new Vector3<TNum>[sourceVertices.Length];
        var indices = sourceIndices[..];
        for (var i = 0; i < sourceVertices.Length; i++) vertices[i] = (sourceVertices[i] - around) * scalar+sourceVertices[i];
        return new(vertices, indices);
    }
}

// using System;
// using System.Numerics;
//
// namespace MeshWiz.Math
// {
//     public class MeshTransforms
//     {
//         /// <summary>
//         /// Creates offset direction that when multiplied with a scalar can offset the mesh's vertices
//         /// so that every triangle will be moved alongside its normal by the scalar.
//         /// </summary>
//         /// <typeparam name="TNum">Floating-point numeric type</typeparam>
//         /// <param name="mesh">Indexed mesh to process</param>
//         /// <returns>Array of unit offset directions per vertex</returns>
//         public Vector3<TNum>[] CreateVertexOffsetDirections<TNum>(IIndexedMesh3<TNum> mesh)
//             where TNum : unmanaged, IFloatingPointIeee754<TNum>
//         {
//             var vertices = mesh.Vertices;
//             var indices = mesh.Indices;
//             int vertexCount = vertices.Length;
//
//             // Accumulator for normals per vertex
//             var accumNormals = new Vector3<TNum>[vertexCount];
//             for (int i = 0; i < vertexCount; i++)
//             {
//                 accumNormals[i] = Vector3<TNum>.Zero;
//             }
//
//             // Compute triangle normals and accumulate
//             for (int t = 0; t < indices.Length; t++)
//             {
//                 var tri = indices[t];
//                 int i0 = tri.I0, i1 = tri.I1, i2 = tri.I2;
//                 var v0 = vertices[i0];
//                 var v1 = vertices[i1];
//                 var v2 = vertices[i2];
//
//                 // Edge vectors
//                 var edge1 = v1 - v0;
//                 var edge2 = v2 - v0;
//
//                 // Unnormalized normal
//                 var normal = Vector3.Cross(edge1, edge2);
//                 var length = normal.Length();
//
//                 if (length > TNum.Zero)
//                 {
//                     // Normalize
//                     normal /= length;
//
//                     // Accumulate for each vertex
//                     accumNormals[i0] += normal;
//                     accumNormals[i1] += normal;
//                     accumNormals[i2] += normal;
//                 }
//             }
//
//             // Create unit direction per vertex
//             var directions = new Vector3<TNum>[vertexCount];
//             for (int i = 0; i < vertexCount; i++)
//             {
//                 var n = accumNormals[i];
//                 var len = n.Length();
//                 if (len > TNum.Zero)
//                 {
//                     directions[i] = n / len;
//                 }
//                 else
//                 {
//                     // Fallback if isolated vertex: no offset
//                     directions[i] = Vector3<TNum>.Zero;
//                 }
//             }
//
//             return directions;
//         }
//
//         public IndexedMesh3<TNum> GrowMeshBy<TNum>(IIndexedMesh3<TNum> mesh, TNum distance,
//             Vector3<TNum>[] offsetDirections)
//             where TNum : unmanaged, IFloatingPointIeee754<TNum>
//         {
//             var sourceVertices = mesh.Vertices;
//             var sourceIndices = mesh.Indices;
//             var vertices = new Vector3<TNum>[sourceVertices.Length];
//             var indices = new TriangleIndexer[sourceIndices.Length];
//
//             Array.Copy(sourceVertices, vertices, sourceVertices.Length);
//             for (var i = 0; i < sourceVertices.Length; i++)
//             {
//                 vertices[i] = sourceVertices[i] + offsetDirections[i] * distance;
//             }
//
//             return new(vertices, indices);
//         }
//
//         public IndexedMesh3<TNum> GrowMeshBy<TNum>(IIndexedMesh3<TNum> mesh,
//             TNum distance,
//             Vector3<TNum>[] offsetDirections,
//             bool alongX,
//             bool alongY,
//             bool alongZ)
//             where TNum : unmanaged, IFloatingPointIeee754<TNum>
//         {
//             if (alongX && alongY && alongZ)
//                 return GrowMeshBy(mesh, distance, offsetDirections);
//
//             Vector3<TNum> mask = new(
//                 alongX ? TNum.One : TNum.Zero,
//                 alongY ? TNum.One : TNum.Zero,
//                 alongZ ? TNum.One : TNum.Zero);
//
//             var sourceVertices = mesh.Vertices;
//             var sourceIndices = mesh.Indices;
//             var vertices = new Vector3<TNum>[sourceVertices.Length];
//             var indices = new TriangleIndexer[sourceIndices.Length];
//
//             Array.Copy(sourceVertices, vertices, sourceVertices.Length);
//             for (var i = 0; i < sourceVertices.Length; i++)
//             {
//                 var offset = offsetDirections[i] * distance;
//                 offset = new(
//                     offset.X * mask.X,
//                     offset.Y * mask.Y,
//                     offset.Z * mask.Z);
//                 vertices[i] = sourceVertices[i] + offset;
//             }
//
//             return new(vertices, indices);
//         }
//     }
// }