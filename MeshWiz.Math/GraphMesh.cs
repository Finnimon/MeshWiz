using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using MeshWiz.Collections;
using MeshWiz.RefLinq;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public static partial class Mesh
{
    public sealed class Graph<TMesh, TNum>
        where TMesh : IMesh<TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        public readonly TMesh Mesh;
        private readonly int[] _edgeStart;
        public ReadOnlySpan<int> EdgeStarts => _edgeStart;
        private readonly int[] _edgeCount;
        public ReadOnlySpan<int> EdgeCounts => _edgeCount;
        private readonly int[] _edges;

        internal Graph(TMesh triangles, int[] edgeStart, int[] edgeCount, int[] edges)
        {
            Mesh = triangles;
            _edgeStart = edgeStart;
            _edgeCount = edgeCount;
            _edges = edges;
        }

        public Graph(TMesh triangles, IEnumerable<int> edgeStart,
            IEnumerable<int> edgeCount,
            IEnumerable<int> edges)
        {
            Mesh = triangles;
            _edgeStart = edgeStart.ToArray();
            _edgeCount = edgeCount.ToArray();
            _edges = edges.ToArray();
        }

        public ReadOnlySpan<int> Edges => _edges;


        public ReadOnlySpan<int> GetEdges(int triangleIndex) =>
            Edges.Slice(_edgeStart[triangleIndex], _edgeCount[triangleIndex]);
        public SmartSelectIterator<int, Triangle3<TNum>> GetNeighbors(int triangle) =>
            GetEdges(triangle).Select(i => Mesh[i]);

        public static Graph<TMesh, TNum> CreateVertexBased(TMesh tris)
        {
            if (tris.Count == 0) return new Graph<TMesh, TNum>(tris, [], [], []);
            Dictionary<Vec3<TNum>, List<int>> mappings = [];
            for (var i = 0; i < tris.Count; i++)
            {
                var tri = tris[i];
                WriteEntry(mappings, tri.A, i);
                WriteEntry(mappings, tri.B, i);
                WriteEntry(mappings, tri.C, i);
            }

            HashSet<int> distincter = [];
            List<int> edges = new(tris.Count * 6);
            var edgeStart = GC.AllocateUninitializedArray<int>(tris.Count);
            var edgeCount = GC.AllocateUninitializedArray<int>(tris.Count);
            for (var i = 0; i < tris.Count; i++)
            {
                var tri = tris[i];
                edgeStart[i] = edges.Count;
                edgeCount[i] = ReadEntry(mappings, tri.A, i, edges, distincter)
                               + ReadEntry(mappings, tri.B, i, edges, distincter)
                               + ReadEntry(mappings, tri.C, i, edges, distincter);
            }

            return new Graph<TMesh, TNum>(tris, edgeStart, edgeCount, edges.ToArray());
        }

        private static int ReadEntry(
            Dictionary<Vec3<TNum>, List<int>> dict,
            Vec3<TNum> vert,
            int index,
            List<int> edges,
            HashSet<int> distincter)
        {
            distincter.UnionWith(dict[vert]);
            distincter.Remove(index);
            edges.AddRange(distincter);
            var count = distincter.Count;
            distincter.Clear();
            return count;
        }

        private static void WriteEntry(Dictionary<Vec3<TNum>, List<int>> dict, Vec3<TNum> vert, int index)
        {
            if (dict.TryGetValue(vert, out var list))
            {
                list.Add(index);
                return;
            }

            dict.Add(vert, [index]);
        }
    }
}