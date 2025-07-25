using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace MeshWiz.Math.Benchmark;

public class Ray3Bench<TNum> 
    where TNum: unmanaged, IFloatingPointIeee754<TNum>
{
    private BvhMesh3<TNum>? BvhMesh { get; set; }

    private Mesh3<TNum>? Mesh { get; set; }
    private Ray3<TNum> _ray;
    [GlobalSetup]
    public void Setup()
    {
        Mesh = new(Sphere<TNum>.GenerateTessellation(Vector3<TNum>.Zero, TNum.One, 512, 1024));
        BvhMesh = new BvhMesh3<TNum>(Mesh);
        _ray = new(Vector3<TNum>.One*TNum.CreateTruncating(100), Mesh.VolumeCentroid);
    }

    [Benchmark]
    public TNum DirectHitTest()
    {
        var ray = _ray;
        
        var distance=TNum.NegativeInfinity;
        
        for (var i = 0; i < Mesh!.Count; i++)
        {
             if(!ray.Intersect(Mesh[i], out var curDistance)) continue;
             distance = curDistance;
        }
        
        return distance;
    }

    [Benchmark]
    public TNum BvhHitTest()
    {
        BvhMesh!.Intersect(_ray,out TNum t);
        return t;
    }
}