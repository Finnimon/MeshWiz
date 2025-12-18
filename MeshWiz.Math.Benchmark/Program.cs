using BenchmarkDotNet.Running;
using MeshWiz.Math.Benchmark;


BenchmarkRunner.Run<GeodesicBench<double>>();