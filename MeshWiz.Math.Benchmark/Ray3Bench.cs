using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace MeshWiz.Math.Benchmark;

public class Ray3Bench<TNum> 
    where TNum: unmanaged, IFloatingPointIeee754<TNum>
{
    private BvhMesh<TNum>? BvhMesh { get; set; }

    private Mesh<TNum>? Mesh { get; set; }
    private Ray3<TNum> _ray;
    [GlobalSetup]
    public void Setup()
    {
        var iMesh = new Sphere<TNum>(Vector3<TNum>.Zero, TNum.One).Tessellate(512, 1024);
        Mesh = new(iMesh.ToArray());
        BvhMesh = BvhMesh<TNum>.SurfaceAreaHeuristic(iMesh);
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