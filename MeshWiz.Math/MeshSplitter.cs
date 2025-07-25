using System.Numerics;

namespace MeshWiz.Math;

public static class MeshSplitter
{
 public static IndexedMesh3<TNum>[] Split<TNum>(IIndexedMesh3<TNum> mesh)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var triangleCount = mesh.Indices.Length;
        var visited = new bool[triangleCount];

        // Step 1: Build a mapping from vertex index -> triangle indices that use it
        var vertexToTriangles = new Dictionary<uint, List<int>>();
        for (int triIndex = 0; triIndex < triangleCount; triIndex++)
        {
            var tri = mesh.Indices[triIndex];
            foreach (var v in new[] { tri.A, tri.B, tri.C })
            {
                if (!vertexToTriangles.TryGetValue(v, out var list))
                    vertexToTriangles[v] = list = new();
                list.Add(triIndex);
            }
        }

        var islands = new List<IndexedMesh3<TNum>>();

        // Step 2: Find connected components
        for (int i = 0; i < triangleCount; i++)
        {
            if (visited[i]) continue;

            var componentTris = new List<int>();
            var queue = new Queue<int>();
            queue.Enqueue(i);
            visited[i] = true;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                componentTris.Add(current);

                var currentTri = mesh.Indices[current];
                foreach (var v in new[] { currentTri.A, currentTri.B, currentTri.C })
                {
                    foreach (var neighbor in vertexToTriangles[v])
                    {
                        if (!visited[neighbor])
                        {
                            visited[neighbor] = true;
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            // Step 3: Build a new IndexedMesh3<TNum> from this component
            var oldToNew = new Dictionary<uint, uint>();
            var newVertices = new List<Vector3<TNum>>();
            var newIndices = new List<TriangleIndexer>();

            foreach (var triIndex in componentTris)
            {
                var tri = mesh.Indices[triIndex];

                uint GetOrAdd(uint oldIndex)
                {
                    if (!oldToNew.TryGetValue(oldIndex, out var newIndex))
                    {
                        newIndex = (uint)newVertices.Count;
                        newVertices.Add(mesh.Vertices[oldIndex]);
                        oldToNew[oldIndex] = newIndex;
                    }
                    return newIndex;
                }

                var a = GetOrAdd(tri.A);
                var b = GetOrAdd(tri.B);
                var c = GetOrAdd(tri.C);
                newIndices.Add(new TriangleIndexer(a, b, c));
            }

            islands.Add(new IndexedMesh3<TNum>(newVertices.ToArray(), newIndices.ToArray()));
        }

        return islands.ToArray();
    }
}