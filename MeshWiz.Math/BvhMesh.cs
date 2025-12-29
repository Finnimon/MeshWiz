using System.Numerics;
using System.Runtime.CompilerServices;
using MeshWiz.Collections;
using MeshWiz.Utility;

namespace MeshWiz.Math;

public class BvhMesh<TNum> : IIndexedMesh<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public Vec3<TNum> VertexCentroid => _vertexCentroid ??= Mesh.Math.VertexCentroid(this);
    public Vec3<TNum> SurfaceCentroid => _surfaceCentroid ??= Mesh.Math.SurfaceCentroid(this).XYZ;
    public Vec3<TNum> VolumeCentroid => _volumeCentroid ??= Mesh.Math.VolumeCentroid(this).XYZ;
    /// <summary>
    /// Depth of the recursive Tree in <see cref="_hierarchy"/>
    /// </summary>
    public readonly uint Depth;
    public TNum Volume => _volume ??= Mesh.Math.Volume(this);
    public TNum SurfaceArea => _surfaceArea ??= Mesh.Math.SurfaceArea(this);
    public AABB<Vec3<TNum>> BBox { get; }

    private TNum? _surfaceArea;
    private TNum? _volume;
    private Vec3<TNum>? _vertexCentroid;
    private Vec3<TNum>? _surfaceCentroid;
    private Vec3<TNum>? _volumeCentroid;


    public TriangleIndexer[] Indices { get; }
    public Vec3<TNum>[] Vertices { get; }
    public int Count => Indices.Length;
    public Triangle3<TNum> this[int index] => Indices[index].Extract(Vertices);


    private readonly BoundedVolume<TNum>[] _hierarchy;
    public ReadOnlySpan<BoundedVolume<TNum>> Hierarchy => _hierarchy;

    public static BvhMesh<TNum> SurfaceAreaHeuristic(IReadOnlyList<Triangle3<TNum>> triangles)
    {
        var info = Mesh.Bvh.HierarchizeSah(triangles);
        var hierarchy = info.hierarchy;
        hierarchy.Trim();
        return new(hierarchy.GetUnsafeAccess(), info.indices, info.vertices, info.depth);
    }

    private BvhMesh(BoundedVolume<TNum>[] hierarchy, TriangleIndexer[] indices, Vec3<TNum>[] vertices, uint depth)
    {
        _hierarchy = hierarchy;
        Indices = indices;
        Vertices = vertices;
        BBox = _hierarchy.Length > 0 ? _hierarchy[0].Bounds : AABB<Vec3<TNum>>.Empty;
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
        where THitTester : IIntersecter<Triangle3<TNum>, TNum>, IIntersecter<AABB<Vec3<TNum>>, TNum>
    {
        var nodeToTest = HitTestStack();
        var count = 1;
        result = TNum.PositiveInfinity;
        while (TryPop(nodeToTest, ref count, out var nodeIndex))
        {
            ref readonly var node = ref _hierarchy[nodeIndex];
            if (!tester.Intersect(node.Bounds, out var boundsResult)) continue;
            if (boundsResult > result) continue;
            if (node.IsParent)
            {
                Push(nodeToTest, ref count, node.FirstChild);
                Push(nodeToTest, ref count, node.SecondChild);
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


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Push(int[] nodeToTest, ref int count, int nodeFirstChild) =>
        nodeToTest[count++] = nodeFirstChild;

    private static bool TryPop(int[] nodeToTest, ref int count, out int o)
    {
        o = 0;
        if (count < 1) return false;
        o = nodeToTest[--count];
        return true;
    }

    public bool DoesIntersect<TIntersecter>(TIntersecter tester)
        where TIntersecter : IIntersecter<Triangle3<TNum>, TNum>, IIntersecter<AABB<Vec3<TNum>>, TNum>
    {
        var nodeToTest = HitTestStack();
        var count = 1;
        while (TryPop(nodeToTest, ref count, out var nodeIndex))
        {
            ref readonly var node = ref _hierarchy[nodeIndex];
            if (!tester.DoIntersect(node.Bounds)) continue;
            if (node.IsParent)
            {
                Push(nodeToTest, ref count, node.FirstChild);
                Push(nodeToTest, ref count, node.SecondChild);
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
        where TIntersecter : IIntersecter<Triangle3<TNum>, TNum>, IIntersecter<AABB<Vec3<TNum>>, TNum>
    {
        var nodeToTest = HitTestStack();
        var count = 1;
        triangleIndex = -1;
        var distance = TNum.PositiveInfinity;
        while (TryPop(nodeToTest, ref count, out var nodeIndex))
        {
            ref readonly var node = ref _hierarchy[nodeIndex];
            if (!tester.Intersect(node.Bounds, out var boundsResult)) continue;
            if (boundsResult > distance) continue;
            if (node.IsParent)
            {
                Push(nodeToTest, ref count, node.FirstChild);
                Push(nodeToTest, ref count, node.SecondChild);
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
        where TIntersecter : IIntersecter<Triangle3<TNum>, TNum>, IIntersecter<AABB<Vec3<TNum>>, TNum>
    {
        var nodeToTest = HitTestStack();
        var count = 1;
        List<BvhHitInfo<TNum>> hitMemory = [];
        while (TryPop(nodeToTest, ref count, out var nodeIndex))
        {
            ref readonly var node = ref _hierarchy[nodeIndex];
            if (!tester.DoIntersect(node.Bounds)) continue;
            if (node.IsParent)
            {
                Push(nodeToTest, ref count, node.FirstChild);
                Push(nodeToTest, ref count, node.SecondChild);
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

    private int[] HitTestStack() => new int[GetDepthFirstStackSize()];


    public Polyline<Vec2<TNum>, TNum>[] IntersectRolling(Plane3<TNum> plane)
    {
        RollingList<int> nodeToTest = new(capacity: GetDepthFirstStackSize()) { 0 };
        RollingList<Line<Vec2<TNum>, TNum>> intersections = [];
        while (nodeToTest.TryPopBack(out var nodeIndex))
        {
            ref readonly var node = ref _hierarchy[nodeIndex];
            if (!plane.DoIntersect(node.Bounds)) continue;
            if (node.IsParent)
            {
                nodeToTest.PushBack(node.FirstChild);
                nodeToTest.PushBack(node.SecondChild);
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
    
    public RollingList<Line<Vec2<TNum>, TNum>> IntersectAll(Plane3<TNum> plane)
    {
        RollingList<int> nodeToTest = new(capacity: GetDepthFirstStackSize()) { 0 };
        RollingList<Line<Vec2<TNum>, TNum>> intersections = [];
        while (nodeToTest.TryPopBack(out var nodeIndex))
        {
            ref readonly var node = ref _hierarchy[nodeIndex];
            if (!plane.DoIntersect(node.Bounds)) continue;
            if (node.IsParent)
            {
                nodeToTest.PushBack(node.FirstChild);
                nodeToTest.PushBack(node.SecondChild);
                continue;
            }

            for (var triangleIndex = node.Start; triangleIndex < node.End; triangleIndex++)
            {
                var tri = this[triangleIndex];

                if (!plane.Intersect(tri, out var line)) continue;

                intersections.PushBack(plane.ProjectIntoLocal(line));
            }
        }

        return intersections;
    }

    private int GetDepthFirstStackSize() => (int)Depth * 2 + 1;
    
    public BvhMesh<TNum> Indexed() => this;

    public BvhMesh<TOther> To<TOther>()
        where TOther : unmanaged, IFloatingPointIeee754<TOther>
    {
        var indices = Indices[..^1];
        var vertices = new Vec3<TOther>[Vertices.Length];
        for (var i = 0; i < Vertices.Length; i++) vertices[i] = Vertices[i].To<TOther>();
        var hierarchy = new BoundedVolume<TOther>[_hierarchy.Length];
        for (var index = 0; index < _hierarchy.Length; index++)
        {
            var node = _hierarchy[index];
            var bbox = node.Bounds;
            AABB<Vec3<TOther>> oBBox = bbox.To<Vec3<TOther>>();
            BoundedVolume<TOther> otherNode = node.IsParent
                ? BoundedVolume<TOther>.MakeParent(oBBox, node.FirstChild, node.SecondChild)
                : BoundedVolume<TOther>.MakeLeaf(oBBox, node.Start, node.Length);
            hierarchy[index] = otherNode;
        }

        return new BvhMesh<TOther>(hierarchy, indices, vertices, Depth);
    }
}