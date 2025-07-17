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
    public BvhMesh3<TNum> HierarchizeBenchBigMesh()
        => new(_mesh!);
}