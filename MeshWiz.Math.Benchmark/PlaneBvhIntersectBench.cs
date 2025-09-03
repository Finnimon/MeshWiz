using BenchmarkDotNet.Attributes;
using MeshWiz.IO;
using MeshWiz.IO.Stl;

namespace MeshWiz.Math.Benchmark;

public class PlaneBvhIntersectBench
{
    private BvhMesh<float>? _mesh;
    private Plane3<float> _plane;
    [GlobalSetup]
    public void Setup()
    {
        Triangle3<float>[] tris =
        [
            .. new Sphere<float>(-Vector3<float>.One, 1f).Tessellate(),
            // ..Sphere<float>.GenerateTessellation(Vector3<float>.One * 2, 1, 256, 512)
        ];
        tris = MeshIO.ReadFile<FastStlReader, float>("/home/finnimon/source/repos/TestFiles/drag.stl").ToArray();
        _mesh = new BvhMesh<float>(tris);
        _plane = new Plane3<float>(Vector3<float>.UnitZ, _mesh.VolumeCentroid);
            
    }
    [Benchmark]
    public Polyline<Vector2<float>, float>[] BenchIntersectionRolling()
    {
        var mesh=_mesh!;
        return mesh.IntersectRolling(_plane);
        }//1(200*10^-6)
    // [Benchmark]
    // public PolyLine<Vector3<float>, float>[] BigBenchIntersection()
    // {
    //     var mesh=_bigMesh!;
    //     return mesh.Intersect(_plane);
    // }
    // [Benchmark]
    // public PolyLine<Vector3<float>, float>[] BigBenchIntersectionRolling()
    // {
    //     var mesh=_bigMesh!;
    //     return mesh.IntersectRolling(_plane);
    // }
}