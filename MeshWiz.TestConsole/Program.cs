

using MeshWiz.RefLinq;
using MeshWiz.RefLinq.Benchmark;
using MeshWiz.Utility.Extensions;

Console.WriteLine(2.NextPow2());
var bench = new RefIterBench() { N = 100_00 };
bench.Setup();
var correct = bench.Linq6SelMany().SequenceEqual(bench.RefLinq6WhereSelMany());
Console.WriteLine(correct);