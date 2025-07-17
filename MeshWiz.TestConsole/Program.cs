// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using MeshWiz.IO;
using MeshWiz.IO.Stl;
using MeshWiz.Math;
using MeshWiz.Slicer;
using MeshWiz.TestConsole;
using OpenTK.Graphics.OpenGL;

var mesh= new Mesh3<double>(new Sphere<double>(Vector3<double>.Zero,1d).TessellatedSurface);

var bvh=new BvhMesh3<double>(mesh);