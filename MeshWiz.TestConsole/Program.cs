using System.Runtime.Intrinsics;
using MeshWiz.Math;
using OpenTK.Compute.OpenCL;

Console.WriteLine("Hello World!");
Console.WriteLine(Vector128.IsHardwareAccelerated);
var circle=new Circle3<float>(Vector3<float>.Zero, Vector3<float>.UnitY, 1);
var arcSection= circle.ArcSection(0, 3.14f);
Console.WriteLine(arcSection.ToPolyline());