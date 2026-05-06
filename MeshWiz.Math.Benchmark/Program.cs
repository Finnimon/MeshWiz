using System.Runtime.Intrinsics;
using BenchmarkDotNet.Running;
using MeshWiz.Math;
using MeshWiz.Math.Benchmark;

BenchmarkRunner.Run<Vec3Length>();
// BenchmarkRunner.Run<BvhConstruction<double>>();
// BvhIntersect t=new();
// t.Setup();
// for(var i=0;i<10000;i++)
//     t.GenericBvh();