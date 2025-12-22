    

using BenchmarkDotNet.Engines;
using MeshWiz.Math;
using MeshWiz.Math.Benchmark;
using MeshWiz.RefLinq;
using MeshWiz.Signals;

var signal=new FSignal<double,double>(double.Sin).Cache();
var best= Signal.Analysis.BestFitBinary(signal,-1d,AABB.From(0d,6));
Console.WriteLine(best);
Console.WriteLine(signal.Cache.Count);
var best2= Signal.Analysis.BestFitNewton(signal,-1d,AABB.From(0d,6));
Console.WriteLine(best2);
Console.WriteLine(signal.Cache.Count);