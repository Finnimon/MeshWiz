using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace MeshWiz.Math.Benchmark;

[MemoryDiagnoser]
public class BvhAlgoBench<TNum>
where TNum:unmanaged,IFloatingPointIeee754<TNum>
{
    private IMesh<TNum>? _mesh;

    [Params(32)]
    public int MaxDepth;
    [Params(4)]
    public int SplitTests;

    [GlobalSetup]
    public void Setup() 
        => _mesh= new Sphere<TNum>(Vec3<TNum>.Zero, TNum.One).Tessellate(64,128);

    [Benchmark]
    public void SahGeneric() => Bvh.Create.Sah<Triangle3<TNum>,Vec3<TNum>, TNum>(_mesh!, MaxDepth, SplitTests);
    [Benchmark]
    public void SahGenericNonReordering() => Bvh.Create.SahNonReordering<Triangle3<TNum>,Vec3<TNum>, TNum>(_mesh!, MaxDepth, SplitTests);
    [Benchmark]
    public void GenericFast()=>Bvh.Create.LinearNonReordering<Triangle3<TNum>, Vec3<TNum>, TNum>(_mesh!, MaxDepth, SplitTests);
    
    [Benchmark]
    public (BoundedVolumeHierarchy<TNum> hierarchy, TriangleIndexer[] indices, Vec3<TNum>[] vertices, uint d) SahSpecified() 
        => Mesh.Bvh.HierarchizeSah(_mesh!, (uint)MaxDepth,(uint)SplitTests);
}