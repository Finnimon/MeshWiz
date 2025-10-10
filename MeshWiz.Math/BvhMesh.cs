using System.Numerics;
using MeshWiz.Utility;

namespace MeshWiz.Math;

public class BvhMesh<TNum> : IIndexedMesh<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public Vector3<TNum> VertexCentroid => _vertexCentroid ??= Mesh.Math.VertexCentroid(this);
    public Vector3<TNum> SurfaceCentroid => _surfaceCentroid ??= Mesh.Math.SurfaceCentroid(this).XYZ;
    public Vector3<TNum> VolumeCentroid => _volumeCentroid ??= Mesh.Math.VolumeCentroid(this).XYZ;
    public readonly uint Depth;
    public TNum Volume => _volume ??= Mesh.Math.Volume(this);
    public TNum SurfaceArea => _surfaceArea ??= Mesh.Math.SurfaceArea(this);
    public AABB<Vector3<TNum>> BBox { get; }

    private TNum? _surfaceArea;
    private TNum? _volume;
    private Vector3<TNum>? _vertexCentroid;
    private Vector3<TNum>? _surfaceCentroid;
    private Vector3<TNum>? _volumeCentroid;


    public TriangleIndexer[] Indices { get; }
    public Vector3<TNum>[] Vertices { get; }
    public int Count => Indices.Length;
    public Triangle3<TNum> this[int index] => Indices[index].Extract(Vertices);


    public readonly BoundedVolume<TNum>[] Hierarchy;


    public static BvhMesh<TNum> SurfaceAreaHeuristic(IReadOnlyList<Triangle3<TNum>> triangles)
    {
        var info = Mesh.Bvh.HierarchizeSah(triangles);
        var hierarchy = info.hierarchy;
        hierarchy.Trim();
        return new(hierarchy.GetUnsafeAccess(), info.indices, info.vertices, info.depth);
    }

    private BvhMesh(BoundedVolume<TNum>[] hierarchy, TriangleIndexer[] indices, Vector3<TNum>[] vertices, uint depth)
    {
        Hierarchy = hierarchy;
        Indices = indices;
        Vertices = vertices;
        BBox = Hierarchy.Length > 0 ? Hierarchy[0].Bounds : AABB<Vector3<TNum>>.Empty;
        Depth = depth;
    }


    public void InitializeLazies()
    {
        var info = Mesh.Math.AllInfo(this);
        _vertexCentroid = info.VertexCentroid;
        _surfaceCentroid = info.SurfaceCentroid;
        _volumeCentroid = info.VolumeCentroid;
        _surfaceArea = info.SurfaceArea;
        _volume = info.Volume;
    }


    public bool Intersect<THitTester>(THitTester tester, out TNum result)
        where THitTester : IIntersecter<Triangle3<TNum>, TNum>, IIntersecter<AABB<Vector3<TNum>>, TNum>
    {
        Stack<int> nodeToTest = HitTestStack();
        result = TNum.PositiveInfinity;
        while (nodeToTest.TryPop(out var nodeIndex))
        {
            ref readonly var node = ref Hierarchy[nodeIndex];
            if (!tester.Intersect(node.Bounds, out var boundsResult)) continue;
            if (boundsResult > result) continue;
            if (node.IsParent)
            {
                nodeToTest.Push(node.FirstChild);
                nodeToTest.Push(node.SecondChild);
                continue;
            }

            for (var i = node.Start; i < node.End; i++)
            {
                var tri = this[i];
                if (!tester.Intersect(tri, out var triResult)) continue;
                result = TNum.Min(triResult, result);
            }
        }

        return result < TNum.PositiveInfinity;
    }


    public bool DoesIntersect<TIntersecter>(TIntersecter tester)
        where TIntersecter : IIntersecter<Triangle3<TNum>, TNum>, IIntersecter<AABB<Vector3<TNum>>, TNum>
    {
        Stack<int> nodeToTest = HitTestStack();
        while (nodeToTest.TryPop(out var nodeIndex))
        {
            ref readonly var node = ref Hierarchy[nodeIndex];
            if (!tester.DoIntersect(node.Bounds)) continue;
            if (node.IsParent)
            {
                nodeToTest.Push(node.FirstChild);
                nodeToTest.Push(node.SecondChild);
                continue;
            }

            for (var i = node.Start; i < node.End; i++)
            {
                var tri = this[i];
                if (tester.DoIntersect(tri)) return true;
            }
        }

        return false;
    }

    public bool Intersect<TIntersecter>(TIntersecter tester, out int triangleIndex)
        where TIntersecter : IIntersecter<Triangle3<TNum>, TNum>, IIntersecter<AABB<Vector3<TNum>>, TNum>
    {
        Stack<int> nodeToTest = HitTestStack();
        triangleIndex = -1;
        var distance = TNum.PositiveInfinity;
        while (nodeToTest.TryPop(out var nodeIndex))
        {
            ref readonly var node = ref Hierarchy[nodeIndex];
            if (!tester.Intersect(node.Bounds, out var boundsResult)) continue;
            if (boundsResult > distance) continue;
            if (node.IsParent)
            {
                nodeToTest.Push(node.FirstChild);
                nodeToTest.Push(node.SecondChild);
                continue;
            }

            for (var i = node.Start; i < node.End; i++)
            {
                var tri = this[i];
                if (!tester.Intersect(tri, out var triResult)) continue;
                if (distance < triResult) continue;
                triangleIndex = i;
                distance = triResult;
            }
        }

        return triangleIndex >= 0;
    }


    public bool Intersect<TIntersecter>(TIntersecter tester, out BvhHitInfo<TNum>[] hits)
        where TIntersecter : IIntersecter<Triangle3<TNum>, TNum>, IIntersecter<AABB<Vector3<TNum>>, TNum>
    {
        var nodeToTest = HitTestStack();
        List<BvhHitInfo<TNum>> hitMemory = [];
        while (nodeToTest.TryPop(out var nodeIndex))
        {
            ref readonly var node = ref Hierarchy[nodeIndex];
            if (!tester.DoIntersect(node.Bounds)) continue;
            if (node.IsParent)
            {
                nodeToTest.Push(node.FirstChild);
                nodeToTest.Push(node.SecondChild);
                continue;
            }

            for (var triangleIndex = node.Start; triangleIndex < node.End; triangleIndex++)
            {
                var tri = this[triangleIndex];
                if (!tester.Intersect(tri, out var distance)) continue;
                hitMemory.Add(new(distance, triangleIndex));
            }
        }

        hits = hitMemory.ToArray();
        return hitMemory.Count > 0;
    }

    public Stack<int> HitTestStack()
    {
        Stack<int> nodeToTest = new((int)Depth * 2);
        nodeToTest.Push(0);
        return nodeToTest;
    }


    public Polyline<Vector2<TNum>, TNum>[] IntersectRolling(Plane3<TNum> plane)
    {
        RollingList<int> nodeToTest = [0];
        RollingList<Line<Vector2<TNum>, TNum>> intersections = [];
        while (nodeToTest.TryPopFront(out var nodeIndex))
        {
            ref readonly var node = ref Hierarchy[nodeIndex];
            if (!plane.DoIntersect(node.Bounds)) continue;
            if (node.IsParent)
            {
                nodeToTest.PushFront(node.FirstChild);
                nodeToTest.PushFront(node.SecondChild);
                continue;
            }

            for (var triangleIndex = node.Start; triangleIndex < node.End; triangleIndex++)
            {
                var tri = this[triangleIndex];

                if (!plane.Intersect(tri, out var line)) continue;

                intersections.PushBack(plane.ProjectIntoLocal(line));
            }
        }

        return Polyline.Creation.UnifyNonReversing(intersections);
    }


    public BvhMesh<TNum> Indexed() => this;

    public BvhMesh<TOther> To<TOther>()
        where TOther : unmanaged, IFloatingPointIeee754<TOther>
    {
        var indices = Indices[..^1];
        var vertices = new Vector3<TOther>[Vertices.Length];
        for (var i = 0; i < Vertices.Length; i++) vertices[i] = Vertices[i].To<TOther>();
        var hierarchy = new BoundedVolume<TOther>[Hierarchy.Length];
        for (var index = 0; index < Hierarchy.Length; index++)
        {
            var node = Hierarchy[index];
            var bbox = node.Bounds;
            AABB<Vector3<TOther>> oBBox = bbox.To<Vector3<TOther>>();
            BoundedVolume<TOther> otherNode = node.IsParent
                ? BoundedVolume<TOther>.MakeParent(oBBox, node.FirstChild, node.SecondChild)
                : BoundedVolume<TOther>.MakeLeaf(oBBox, node.Start, node.Length);
            hierarchy[index] = otherNode;
        }

        return new BvhMesh<TOther>(hierarchy, indices, vertices, Depth);
    }
}