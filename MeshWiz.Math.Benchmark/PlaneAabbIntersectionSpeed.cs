using BenchmarkDotNet.Attributes;

namespace MeshWiz.Math.Benchmark;

public class PlaneAabbIntersectionSpeed
{
    [Params(0.1f,0.5f,1f)] public float BoxSize;
    public AABB<Vec3<float>> Box;
    [Params(0f,0.2f,1f)] public float D;
    public Plane3<float> Plane;

    [GlobalSetup]
    public void Setup()
    {
        Box = AABB<Vec3<float>>.Around(Vec3<float>.Zero, Vec3<float>.One * BoxSize);
        Plane = new Plane3<float>(new Vec3<float>(0, 0, 1),D);
    }

    [Benchmark(Baseline = true)]
    public bool DistanceSignIntersect()
#pragma warning disable CS0618 // Type or member is obsolete
        => Plane.DoIntersectDistanceSign(Box);
#pragma warning restore CS0618 // Type or member is obsolete
    
    [Benchmark]
    public bool ClampIntersect()
        => Plane.DoIntersect(Box);

}