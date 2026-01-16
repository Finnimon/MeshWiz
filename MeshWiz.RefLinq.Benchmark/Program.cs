// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using MeshWiz.RefLinq.Benchmark;

// var summaries = BenchmarkRunner.Run(typeof(Program).Assembly);
BenchmarkRunner.Run(typeof(Program).Assembly);
