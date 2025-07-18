using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace MeshWiz.Math.Benchmark;

public class BvhMesh3Bench<TNum>
where TNum:unmanaged, IFloatingPointIeee754<TNum>
{
    private Mesh3<TNum>? _mesh;

    [GlobalSetup]
    public void Setup() 
        => _mesh= new Mesh3<TNum>(Sphere<TNum>.GenerateTessellation(Vector3<TNum>.Zero, TNum.One,64,128));

    [Benchmark]
    [Obsolete("Obsolete")]
    public void ObsoleteHierarchize()
    {
        var (indices, vertices) = MeshMath.Indicate(_mesh!);
        var hierarchy=MeshMath.Hierarchize(indices, vertices);
    }

    [Benchmark]
    public BvhMesh3<TNum> OptimizedHierarchize()
    =>new  BvhMesh3<TNum>(_mesh!);
}