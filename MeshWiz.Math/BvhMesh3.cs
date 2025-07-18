using System.Collections;
using System.Collections.Immutable;
using System.Numerics;

namespace MeshWiz.Math;

public class BvhMesh3<TNum> : IIndexedMesh3<TNum> 
    where TNum : unmanaged, IFloatingPointIeee754<TNum> 
{
    
    public Vector3<TNum> Centroid => VolumeCentroid;
    public Vector3<TNum> VertexCentroid => _vertexCentroid ??= MeshMath.VertexCentroid(this);
    public Vector3<TNum> SurfaceCentroid => _surfaceCentroid ??= MeshMath.SurfaceCentroid(this).XYZ;
    public Vector3<TNum> VolumeCentroid => _volumeCentroid ??= MeshMath.VolumeCentroid(this).XYZ;

    public TNum Volume => _volume ??= MeshMath.Volume(this);
    Vector3<TNum> IFace<Vector3<TNum>, TNum>.Centroid => SurfaceCentroid;
    public TNum SurfaceArea => _surfaceArea ??= MeshMath.SurfaceArea(this);
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
    private readonly BoundedVolumeHierarchy<TNum> _hierarchy;

    public BvhMesh3(IReadOnlyList<Triangle3<TNum>> mesh, uint maxDepth=32,uint splitTests=4)
    {
        (Indices, Vertices) = MeshMath.Indicate(mesh);
        _hierarchy = MeshMath.Hierarchize(Indices, Vertices,maxDepth,splitTests);
        BBox = _hierarchy[0].Bounds;
    }
    
    
    

    public void InitializeLazies()
    {
        var info = MeshMath.AllInfo(this);
        _vertexCentroid = info.VertexCentroid;
        _surfaceCentroid = info.SurfaceCentroid;
        _volumeCentroid = info.VolumeCentroid;
        _surfaceArea = info.SurfaceArea;
        _volume = info.Volume;
    }


    public bool HitTest<THitTester>(THitTester tester, out TNum result)
        where THitTester : IHitTester<Triangle3<TNum>, TNum>, IHitTester<BBox3<TNum>,TNum>
    {
        Stack<int> nodeToTest = [];
        nodeToTest.Push(0);
        result = TNum.PositiveInfinity;
        while (nodeToTest.TryPop(out var nodeIndex))
        {
            ref var node=ref _hierarchy[nodeIndex];
            if(!tester.HitTest(node.Bounds, out var boundsResult)) continue;
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
                if(!tester.HitTest(tri,out var triResult)) continue;
                result = TNum.Min(triResult, result);
            }
        }
        return result < TNum.PositiveInfinity;
    }
    
    
    public bool HitTest<THitTester>(THitTester tester)
        where THitTester : IHitTester<Triangle3<TNum>, TNum>, IHitTester<BBox3<TNum>,TNum>
    {
        Stack<int> nodeToTest = [];
        nodeToTest.Push(0);
        while (nodeToTest.TryPop(out var nodeIndex))
        {
            ref var node=ref _hierarchy[nodeIndex];
            if(!tester.HitTest(node.Bounds)) continue;
            if (node.IsParent)
            {
                nodeToTest.Push(node.FirstChild);
                nodeToTest.Push(node.SecondChild);
                continue;
            }

            for (var i = node.Start; i < node.End; i++)
            {
                var tri = this[i];
                if (tester.HitTest(tri)) return true;
            }
        }

        return false;
    }
    
    public bool HitTest<THitTester>(THitTester tester, out int triangleIndex)
        where THitTester : IHitTester<Triangle3<TNum>, TNum>, IHitTester<BBox3<TNum>,TNum>
    {
        Stack<int> nodeToTest = [];
        nodeToTest.Push(0);
        triangleIndex = -1;
        var distance = TNum.PositiveInfinity;
        while (nodeToTest.TryPop(out var nodeIndex))
        {
            ref var node=ref _hierarchy[nodeIndex];
            if(!tester.HitTest(node.Bounds, out var boundsResult)) continue;
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
                if(!tester.HitTest(tri,out var triResult)) continue;
                if(distance<triResult) continue;
                triangleIndex = i;
                distance = triResult;
            }
        }

        return triangleIndex>=0;
    }
    
    public IEnumerator<Triangle3<TNum>> GetEnumerator()
    {
        for (int i = 0; i < Count; i++) yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}