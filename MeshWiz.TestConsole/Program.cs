

using MeshWiz.Math;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

BBox3<double> box=new (-Vector3<double>.One, Vector3<double>.One);
// var baseMesh = box.Tessellate();
var baseMesh = new Sphere<double>(Vector3<double>.Zero, 1).Tessellate();
BvhMesh3<double> indexed=new(baseMesh);
Console.WriteLine(indexed.Count);
var plane = new Plane3<double>(Vector3<double>.UnitZ, 0.0);
var polys= indexed.IntersectRolling(plane);
var poly=polys[0];
// Console.WriteLine($"Num Polylines: {polys.Length}");
// Console.WriteLine($"PolyLineCount:{poly.Count}");
// Console.WriteLine(plane.SignedDistance(Vector3<double>.One));
// Console.WriteLine(poly.Length);


// var rolyPoly=new RollingList<int>();
// Enumerable.Range(0,16).ForEach((i)=>rolyPoly.PushBack(i));
// for(var i=-1;i>=-16;--i)
// {
//     rolyPoly.PushFront(i);
// }
// Console.WriteLine(rolyPoly.Capacity);
// while(rolyPoly.TryPopFront(out var i))
// {
//     Console.WriteLine(i);
// }
// Console.WriteLine(rolyPoly.Count);
// Console.WriteLine(rolyPoly.Capacity);
