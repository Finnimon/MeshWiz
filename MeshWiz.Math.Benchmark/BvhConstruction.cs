using System;
using System.Linq;
using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace MeshWiz.Math.Benchmark;

[MemoryDiagnoser]
public class BvhConstruction<TNum>
where TNum:unmanaged,IFloatingPointIeee754<TNum>
{
    // private const int ShuffleSeed = 0xa047329;
    private Triangle3<TNum>[]? _mesh;


    [Params(32)]
    public int MaxDepth;
    [Params(4)]
    public int SplitTests;

    [GlobalSetup]
    public void Setup()
    {
        var mesh = new Sphere<TNum>(Vec3<TNum>.Zero, TNum.One).Tessellate(256, 256).ToArray();
        // var random = new Random(ShuffleSeed);
        // var shuffle = Enumerable.Range(0, mesh.Length).Select(_ => random.Next()).ToArray();
        // Array.Sort(shuffle,mesh);
        _mesh = mesh;//shuffled for greater reordering cost;
    }

    [Benchmark(Baseline = true)]
    public void SahGeneric() => Bvh.Create.Sah<Triangle3<TNum>,Vec3<TNum>, TNum>(_mesh!, MaxDepth, SplitTests);

    // [Benchmark]
    // public void SahGenericNonReordering() => Bvh.Create.SahNonReordering<Triangle3<TNum>,Vec3<TNum>, TNum>(_mesh!, MaxDepth, SplitTests);
    // [Benchmark]
    // public void GenericFast()=>Bvh.Create.LinearNonReordering<Triangle3<TNum>, Vec3<TNum>, TNum>(_mesh!, MaxDepth, SplitTests);
    //
    [Benchmark]
    [Obsolete]
    public (BoundedVolumeHierarchy<TNum> hierarchy, TriangleIndexer[] indices, Vec3<TNum>[] vertices, uint d) SahSpecified() 
        => Mesh.Bvh.HierarchizeSah(_mesh!, (uint)MaxDepth,(uint)SplitTests);
}