

using BenchmarkDotNet.Engines;
using MeshWiz.Math;
using MeshWiz.Math.Benchmark;
using MeshWiz.RefLinq;
using MeshWiz.Signals;

var signal=new FSignal<double,double>(double.Sin);
var best= Signal.Analysis.BestFitBinary(signal,-1d,AABB.From(0d,6),-1);
var best2= Signal.Analysis.BestFitNewton(signal,-1d,AABB.From(0d,6));
Console.WriteLine(best);Console.WriteLine(best2);