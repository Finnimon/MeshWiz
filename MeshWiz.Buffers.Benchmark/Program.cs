using System.Runtime.CompilerServices;
using BenchmarkDotNet.Running;
using MeshWiz.Buffers;
using MeshWiz.Buffers.Benchmark;
using MeshWiz.RefLinq;
// Console.WriteLine(Unsafe.SizeOf<SegmentedArrayBuilder<int>>());
// Console.WriteLine(Unsafe.SizeOf<BufferedArrayBuilder<int>>());
// BenchmarkRunner.Run<ArrayBuilderBench>();
// BenchmarkRunner.Run<AllocatorBench<int>>();
var bench = new ArrayBuilderBench();
bench.Mode = "Enumerable";
bench.ReferenceType = false;
bench.N = 10;
for (var i = 0; i < 1_000_000; i++)
    bench.BufferToArray();
// BenchmarkDotNet.Running.BenchmarkRunner.Run<ArrayBuilderBench>();
