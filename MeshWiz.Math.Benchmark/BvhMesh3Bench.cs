using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace MeshWiz.Math.Benchmark;

public class BvhMesh3Bench<TNum>
where TNum:unmanaged, IFloatingPointIeee754<TNum>
{
    private IMesh3<TNum>? _mesh;

    [GlobalSetup]
    public void Setup() 
        => _mesh= new Sphere<TNum>(Vector3<TNum>.Zero, TNum.One).Tessellate();

    [Benchmark]
    [Obsolete("Obsolete")]
    public void ObsoleteHierarchize()
    {
        var (indices, vertices) = MeshMath.Indicate(_mesh!);
        var hierarchy=MeshMath.Hierarchize(indices, vertices,32,7);
    }

    [Benchmark]
    public BvhMesh3<TNum> OptimizedHierarchize()
    =>new  BvhMesh3<TNum>(_mesh!,32,7);
}