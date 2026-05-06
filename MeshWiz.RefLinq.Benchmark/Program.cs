// See https://aka.ms/new-console-template for more information

using MeshWiz.RefLinq.Benchmark;

SumBench obj = new() { N = 5 };
obj.Setup();
Console.WriteLine(obj.Linq());
Console.WriteLine(obj.RefLinq());
// BenchmarkRunner.Run<SumBench>();