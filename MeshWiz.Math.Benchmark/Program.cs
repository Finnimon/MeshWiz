using BenchmarkDotNet.Running;
using MeshWiz.Math.Benchmark;

BenchmarkRunner.Run<BvhMesh3Bench<float>>();
BenchmarkRunner.Run<BvhMesh3Bench<double>>();
