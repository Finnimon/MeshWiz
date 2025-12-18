// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using MeshWiz.RefLinq.Benchmark;

var summaries = BenchmarkRunner.Run(typeof(Program).Assembly);
