using BenchmarkDotNet.Attributes;
using MeshWiz.IO;
using MeshWiz.IO.Stl;

namespace MeshWiz.Math.Benchmark;

public class PolylineWindingOrderBench
{
    private Polyline<Vector2<float>, float>? _basePl;

    [GlobalSetup]
    public void Setup()
    {
        var mesh= MeshIO.ReadFile<FastStlReader, float>("/home/finnimon/source/repos/TestFiles/artillery-witch.stl");
        BvhMesh<float> bvh = new (mesh);
        var plane = new Plane3<float>(Vector3<float>.UnitY, bvh.VolumeCentroid);
        _basePl = bvh.IntersectRolling(plane).OrderByDescending(x=>x.Length).First();
    }

    [Benchmark]
    public WindingOrder ExtremePoint() => Polyline.Evaluate.GetWindingOrder(_basePl!);
    [Benchmark]
    public WindingOrder SignedArea() => Polyline.Evaluate.GetWindingOrderAreaSign(_basePl!);

}