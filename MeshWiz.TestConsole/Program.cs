// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using MeshWiz.IO;
using MeshWiz.IO.Stl;
using MeshWiz.Math;
using MeshWiz.Slicer;
using MeshWiz.TestConsole;
using OpenTK.Graphics.OpenGL;

var mesh= IMeshReader<float>.ReadFile<FastStlReader>("/home/finnimon/source/repos/TestFiles/artillery-witch.stl").Indexed();
var sw = Stopwatch.StartNew();
Console.WriteLine(MeshSplitter.Split(mesh).Length);
Console.WriteLine(sw.Elapsed);