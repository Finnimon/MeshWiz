using System.Runtime.Intrinsics;
using BenchmarkDotNet.Running;
using MeshWiz.Math;
using MeshWiz.Math.Benchmark;

BenchmarkRunner.Run<BvhAlgoBench<float>>();