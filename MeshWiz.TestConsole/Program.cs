using System.Numerics;
using System.Text;
using MeshWiz.IO;
using MeshWiz.IO.Stl;
using MeshWiz.Math;
using MeshWiz.Slicer;

Console.WriteLine("Hello World!");
// BBox3<double> box=new (-Vector3<double>.One, Vector3<double>.One);
// // var baseMesh = box.Tessellate();
// var baseMesh = new Sphere<double>(Vector3<double>.Zero, 1).Tessellate();
// BvhMesh3<double> indexed=new(baseMesh);
// Console.WriteLine(indexed.Count);
// var plane = new Plane3<double>(Vector3<double>.UnitZ, 0.0);
// var polys= indexed.IntersectRolling(plane);
// var poly=polys[0];
// Console.WriteLine($"Num Polylines: {polys.Length}");
// Console.WriteLine($"PolyLineCount:{poly.Count}");
// Console.WriteLine(plane.SignedDistance(Vector3<double>.One));
// Console.WriteLine(poly.Length);
Console.WriteLine(Vector2<float>.Zero);
var sb = new StringBuilder();
var stl = MeshIO.ReadFile<FastStlReader, float>("/home/finnimon/source/repos/TestFiles/drag.stl");
stl = AABB.Tessellate(AABB<Vector3<float>>.From(-Vector3<float>.One, Vector3<float>.One));
var bvh = new BvhMesh<float>(stl);
var plane = new Plane3<float>(Vector3<float>.UnitY, bvh.VolumeCentroid);
var pl = bvh.IntersectRolling(plane).OrderByDescending(x => x.Length).First();
var concentric= SimpleConcentric.GenPattern(pl, 0.1f).ToArray();
Console.WriteLine(concentric.Length);
// Console.WriteLine(pl);
// Console.WriteLine(pl2);
// Polyline<Vector2<float>,float> pl = new(
//     new(1,1),
//     new(1,-1),
//     new(-1,-1),
//     new(-1,1),
//     new(1,1)
// );
//
// var mesh= MeshIO.ReadFile<FastStlReader, float>("/home/finnimon/source/repos/TestFiles/artillery-witch.stl");
// BvhMesh<float> bvh = new (mesh);
// var plane = new Plane3<float>(Vector3<float>.UnitY, bvh.VolumeCentroid);
// pl = bvh.IntersectRolling(plane).OrderByDescending(x=>x.Length).First();
// Console.WriteLine(pl.Count);
// Console.WriteLine(Polyline.Evaluate.GetWindingOrderExtremePoint(pl));
// Console.WriteLine(Polyline.Evaluate.GetWindingOrderAreaSign(pl));