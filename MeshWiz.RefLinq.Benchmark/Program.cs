// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using MeshWiz.RefLinq.Benchmark;

BenchmarkRunner.Run<RefIterBench>();