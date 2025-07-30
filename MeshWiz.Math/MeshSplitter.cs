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
        var vertexToTriangles = new Dictionary<int, List<int>>();
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
        var visitCount = 0;
        for (var i = 0; i < triangleCount&&visitCount<triangleCount; i++)
        {
            if (visited[i]) continue;
            
            var componentTris = new List<int>();
            var queue = new Queue<int>();
            queue.Enqueue(i);
            visited[i] = true;
            visitCount++;
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                componentTris.Add(current);

                var (a,b,c) = mesh.Indices[current];
                visitCount=VisitVertex(vertexToTriangles, a, visited, queue, visitCount);
                visitCount=VisitVertex(vertexToTriangles, b, visited, queue, visitCount);
                visitCount=VisitVertex(vertexToTriangles, c, visited, queue, visitCount);
            }

            var oldToNew = new Dictionary<int, int>();
            var newVertices = new List<Vector3<TNum>>();
            var newIndices = new List<TriangleIndexer>();

            foreach (var triIndex in componentTris)
            {
                var tri = mesh.Indices[triIndex];

                var a = GetOrAdd(tri.A, oldToNew, newVertices, mesh);
                var b = GetOrAdd(tri.B, oldToNew, newVertices, mesh);
                var c = GetOrAdd(tri.C, oldToNew, newVertices, mesh);
                newIndices.Add(new TriangleIndexer(a, b, c));
            }

            islands.Add(new IndexedMesh3<TNum>(newVertices.ToArray(), newIndices.ToArray()));
        }

        return islands.ToArray();
    }

    private static int VisitVertex(Dictionary<int, List<int>> vertexToTriangles, 
        int vertIndex, 
        bool[] visited, 
        Queue<int> queue,
        int visitCount)
    {
        foreach (var neighbor in vertexToTriangles[vertIndex])
        {
            if (visited[neighbor]) continue;
            visited[neighbor] = true;
            visitCount++;
            queue.Enqueue(neighbor);
        }
        return visitCount;
    }

    private static int GetOrAdd<TNum>(int oldIndex,
        Dictionary<int, int> oldToNew, 
        List<Vector3<TNum>> newVertices, 
        IIndexedMesh3<TNum> mesh) 
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        if (oldToNew.TryGetValue(oldIndex, out var newIndex)) return newIndex;
        newIndex = newVertices.Count;
        newVertices.Add(mesh.Vertices[oldIndex]);
        oldToNew[oldIndex] = newIndex;
        return newIndex;
    }
}