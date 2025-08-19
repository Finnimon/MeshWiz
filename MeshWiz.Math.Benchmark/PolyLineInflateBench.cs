using BenchmarkDotNet.Attributes;
using MeshWiz.IO;
using MeshWiz.IO.Stl;

namespace MeshWiz.Math.Benchmark;

public class PolyLineInflateBench
{
    private Polyline<Vector2<float>, float>? _basePl;

    [Params(-0.02f,0.02f)]
    public float Amounts;
    [GlobalSetup]
    public void Setup()
    {
        var mesh= MeshIO.ReadFile<FastStlReader, float>("/home/finnimon/source/repos/TestFiles/artillery-witch.stl");
        BvhMesh<float> bvh = new (mesh);
        var plane = new Plane3<float>(Vector3<float>.UnitY, bvh.VolumeCentroid);
        _basePl = bvh.IntersectRolling(plane).OrderByDescending(x=>x.Length).First();
    }

    
    [Benchmark]
    public Polyline<Vector2<float>, float> InflateQueue()
    {
        var pl=_basePl!;
        return Polyline.Transforms.InflateClosedDegenerativeBad(pl, Amounts);
    }
    
    [Benchmark]
    public Polyline<Vector2<float>, float> InflateRange()
    {
        var pl=_basePl!;
        return Polyline.Transforms.InflateClosedDegenerative(pl, Amounts);
    }
    
}