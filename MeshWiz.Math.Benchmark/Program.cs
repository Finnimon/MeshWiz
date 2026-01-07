using BenchmarkDotNet.Running;
using MeshWiz.Math.Benchmark;

// VecMathBench<float> b = new();
// b.Setup();
// Console.WriteLine(b.MulOld()==b.VecMul());
BenchmarkRunner.Run<VecMathBench>();