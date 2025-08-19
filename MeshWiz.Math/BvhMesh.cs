using System.Numerics;
using MeshWiz.Utility;

namespace MeshWiz.Math;

public class BvhMesh<TNum> : IIndexedMesh<TNum> 
    where TNum : unmanaged, IFloatingPointIeee754<TNum> 
{
    public Vector3<TNum> VertexCentroid => _vertexCentroid ??= Mesh.Math.VertexCentroid(this);
    public Vector3<TNum> SurfaceCentroid => _surfaceCentroid ??= Mesh.Math.SurfaceCentroid(this).XYZ;
    public Vector3<TNum> VolumeCentroid => _volumeCentroid ??= Mesh.Math.VolumeCentroid(this).XYZ;

    public TNum Volume => _volume ??= Mesh.Math.Volume(this);
    public TNum SurfaceArea => _surfaceArea ??= Mesh.Math.SurfaceArea(this);
    public BBox3<TNum> BBox { get; }

    private TNum? _surfaceArea;
    private TNum? _volume;
    private Vector3<TNum>? _vertexCentroid;
    private Vector3<TNum>? _surfaceCentroid;
    private Vector3<TNum>? _volumeCentroid;

    
    public TriangleIndexer[] Indices { get; }
    public Vector3<TNum>[] Vertices { get; }
    public int Count =>Indices.Length;
    public Triangle3<TNum> this[int index] => Indices[index].Extract(Vertices);
    
    
    public readonly BoundedVolume<TNum>[] Hierarchy;

    public BvhMesh(IReadOnlyList<Triangle3<TNum>> mesh, uint maxDepth=32,uint splitTests=4)
    {
        (var hierarchy, Indices, Vertices) = Mesh.Bvh.Hierarchize(mesh,maxDepth,splitTests);
        hierarchy.Trim();
        Hierarchy = hierarchy.GetUnsafeAccess();
        BBox = Hierarchy.Length>0? Hierarchy[0].Bounds: BBox3<TNum>.NegativeInfinity;
    }

    private BvhMesh(BoundedVolumeHierarchy<TNum> hierarchy, TriangleIndexer[] indices, Vector3<TNum>[] vertices)
    {
        hierarchy.Trim();
        Hierarchy = hierarchy.GetUnsafeAccess();
        Indices=indices;
        Vertices=vertices;
        BBox = Hierarchy.Length>0? Hierarchy[0].Bounds: BBox3<TNum>.NegativeInfinity;
    }
    private BvhMesh(BoundedVolume<TNum>[] hierarchy, TriangleIndexer[] indices, Vector3<TNum>[] vertices)
    {
        Hierarchy = hierarchy;
        Indices=indices;
        Vertices=vertices;
        BBox = Hierarchy.Length>0? Hierarchy[0].Bounds: BBox3<TNum>.NegativeInfinity;
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
        where THitTester : IIntersecter<Triangle3<TNum>, TNum>, IIntersecter<BBox3<TNum>,TNum>
    {
        Stack<int> nodeToTest = HitTestStack();
        result = TNum.PositiveInfinity;
        while (nodeToTest.TryPop(out var nodeIndex))
        {
            ref readonly var node=ref  Hierarchy[nodeIndex];
            if(!tester.Intersect(node.Bounds, out var boundsResult)) continue;
            if(boundsResult>result) continue;
            if (node.IsParent)
            {
                nodeToTest.Push(node.FirstChild);
                nodeToTest.Push(node.SecondChild);
                continue;
            }

            for (var i = node.Start; i < node.End; i++)
            {
                var tri = this[i];
                if(!tester.Intersect(tri,out var triResult)) continue;
                result = TNum.Min(triResult, result);
            }
        }
        return result < TNum.PositiveInfinity;
    }
    
    
    public bool DoesIntersect<TIntersecter>(TIntersecter tester)
        where TIntersecter : IIntersecter<Triangle3<TNum>, TNum>, IIntersecter<BBox3<TNum>,TNum>
    {
        Stack<int> nodeToTest = HitTestStack();
        while (nodeToTest.TryPop(out var nodeIndex))
        {
            ref readonly var node=ref Hierarchy[nodeIndex];
            if(!tester.DoIntersect(node.Bounds)) continue;
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
        where TIntersecter : IIntersecter<Triangle3<TNum>, TNum>, IIntersecter<BBox3<TNum>,TNum>
    {
        Stack<int> nodeToTest = HitTestStack();
        triangleIndex = -1;
        var distance = TNum.PositiveInfinity;
        while (nodeToTest.TryPop(out var nodeIndex))
        {
            ref readonly var node=ref Hierarchy[nodeIndex];
            if(!tester.Intersect(node.Bounds, out var boundsResult)) continue;
            if(boundsResult>distance) continue;
            if (node.IsParent)
            {
                nodeToTest.Push(node.FirstChild);
                nodeToTest.Push(node.SecondChild);
                continue;
            }

            for (var i = node.Start; i < node.End; i++)
            {
                var tri = this[i];
                if(!tester.Intersect(tri,out var triResult)) continue;
                if(distance<triResult) continue;
                triangleIndex = i;
                distance = triResult;
            }
        }

        return triangleIndex>=0;
    }


    public bool Intersect<TIntersecter>(TIntersecter tester, out BvhHitInfo<TNum>[] hits)
        where TIntersecter : IIntersecter<Triangle3<TNum>, TNum>, IIntersecter<BBox3<TNum>, TNum>
    {
        var nodeToTest = HitTestStack();
        List < BvhHitInfo < TNum >> hitMemory= [];
        while (nodeToTest.TryPop(out var nodeIndex))
        {
            ref readonly var node=ref Hierarchy[nodeIndex];
            if(!tester.DoIntersect(node.Bounds)) continue;
            if (node.IsParent)
            {
                nodeToTest.Push(node.FirstChild);
                nodeToTest.Push(node.SecondChild);
                continue;
            }

            for (var triangleIndex = node.Start; triangleIndex < node.End; triangleIndex++)
            {
                var tri = this[triangleIndex];
                if(!tester.Intersect(tri,out var distance)) continue;
                hitMemory.Add(new(distance,triangleIndex));
            }
        }

        hits= hitMemory.ToArray();
        return hitMemory.Count > 0;
    }

    private static Stack<int> HitTestStack()
    {
        Stack<int> nodeToTest = [];
        nodeToTest.Push(0);
        return nodeToTest;
    }

    
    public Polyline<Vector2<TNum>, TNum>[] IntersectRolling(Plane3<TNum> plane)
    {
        RollingList<int> nodeToTest = [0];
        RollingList<Line<Vector2<TNum>,TNum>> intersections= [];
        while (nodeToTest.TryPopFront(out var nodeIndex))
        {
            ref readonly var node=ref Hierarchy[nodeIndex];
            if(!plane.DoIntersect(node.Bounds)) continue;
            if (node.IsParent)
            {
                nodeToTest.PushFront(node.FirstChild);
                nodeToTest.PushFront(node.SecondChild);
                continue;
            }

            for (var triangleIndex = node.Start; triangleIndex < node.End; triangleIndex++)
            {
                var tri = this[triangleIndex];
                
                if(!plane.Intersect(tri,out var line)) continue;
                
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
        var vertices=new Vector3<TOther>[Vertices.Length];
        for (var i = 0; i < Vertices.Length; i++) vertices[i]=Vertices[i].To<TOther>();
        var hierarchy=new BoundedVolume<TOther>[Hierarchy.Length];
        for (var index = 0; index < Hierarchy.Length; index++)
        {
            var node = Hierarchy[index];
            var bbox = node.Bounds;
            BBox3<TOther> oBBox = new(bbox.Min.To<TOther>(), bbox.Max.To<TOther>());
            BoundedVolume<TOther> otherNode = node.IsParent
                ? BoundedVolume<TOther>.MakeParent(oBBox, node.FirstChild, node.SecondChild)
                : BoundedVolume<TOther>.MakeLeaf(oBBox, node.Start, node.Length);
            hierarchy[index]=otherNode;
        }

        return new BvhMesh<TOther>(hierarchy,indices,vertices);
    }
}