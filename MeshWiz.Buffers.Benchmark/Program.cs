using System.Runtime.CompilerServices;
using BenchmarkDotNet.Running;
using MeshWiz.Buffers;
using MeshWiz.Buffers.Benchmark;
using MeshWiz.RefLinq;
// Console.WriteLine(Unsafe.SizeOf<SegmentedArrayBuilder<int>>());
// Console.WriteLine(Unsafe.SizeOf<BufferedArrayBuilder<int>>());
BenchmarkRunner.Run<ArrayBuilderBench>();

// BenchmarkDotNet.Running.BenchmarkRunner.Run<ArrayBuilderBench>();
