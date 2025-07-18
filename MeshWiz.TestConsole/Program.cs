// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using MeshWiz.IO;
using MeshWiz.IO.Stl;
using MeshWiz.Math;
using MeshWiz.Slicer;
using MeshWiz.TestConsole;
using OpenTK.Graphics.OpenGL;

// var mesh = IMeshReader<float>.ReadFile<FastStlReader>("/home/finnimon/source/repos/TestFiles/drag.stl");
var mesh = new Mesh3<float>(Sphere<float>.GenerateTessellation(Vector3<float>.Zero, 1, 128, 256));
Console.WriteLine($"Mesh count: {mesh.Count}");
var sw = Stopwatch.StartNew();
var bvh=new BvhMesh3<float>(mesh,32);
Console.WriteLine(sw.Elapsed);