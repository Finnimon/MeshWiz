using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace MeshWiz.Math.Benchmark;

public class BvhMesh3Bench<TNum>
where TNum:unmanaged, IFloatingPointIeee754<TNum>
{
    private IMesh<TNum>? _mesh;

    [Params(16u,32u,64u)]
    public uint MaxDepth;
    [Params(4u,8u,12u)]
    public uint SplitTests;

    [GlobalSetup]
    public void Setup() 
        => _mesh= new Sphere<TNum>(Vector3<TNum>.Zero, TNum.One).Tessellate();

    [Benchmark]
    [Obsolete("Obsolete")]
    public BoundedVolumeHierarchy<TNum> ObsoleteHierarchize()
    {
        var (indices, vertices) = Mesh.Indexing.Indicate(_mesh!);
        return Mesh.Bvh.Hierarchize(indices, vertices,MaxDepth,SplitTests);
    }

    [Benchmark]
    public (BoundedVolumeHierarchy<TNum> hierarchy, TriangleIndexer[] indices, Vector3<TNum>[] vertices, uint d) OptimizedHierarchize() 
        => Mesh.Bvh.Hierarchize(_mesh!, MaxDepth,SplitTests);
}