using BenchmarkDotNet.Running;
using MeshWiz.Math.Benchmark;

BenchmarkRunner.Run<VectorByRefBench<float>>();
BenchmarkRunner.Run<VectorByRefBench<double>>();
