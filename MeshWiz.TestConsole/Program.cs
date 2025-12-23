using MeshWiz.Math;
using MeshWiz.Signals;

var signal=new FSignal<double,double>(double.Sin).InputGain(0.1).Cache();
// var best= Signal.Analysis.BestFitBinary(signal,-1d,AABB.From(0d,6));
// Console.WriteLine(best);
// Console.WriteLine($"Cache result: Cache Size {signal.Cache.Count} Hits {signal.CacheHits} Misses {signal.CacheMisses}");
// var best2= Signal.Analysis.BestFitNewton(signal,-1d,AABB.From(0d,6));
// Console.WriteLine(best2);
// Console.WriteLine($"Cache result: Cache Size {signal.Cache.Count} Hits {signal.CacheHits} Misses {signal.CacheMisses}");
//
// var best3= Signal.Analysis.BestFitNewtonRetrying(signal,-1d,AABB.From(1,1.1),AABB.From(0d,6)
// ,maxTries:100);
// Console.WriteLine(best3);
// Console.WriteLine($"Cache result: Cache Size {signal.Cache.Count} Hits {signal.CacheHits} Misses {signal.CacheMisses}");
var best4=Signal.Analysis.BestFitSweepAdaptive(signal,-1,0,AABB.From(-1000,1000d),1d,1048);
Console.WriteLine(best4);
Console.WriteLine($"Cache result: Cache Size {signal.Cache.Count} Hits {signal.CacheHits} Misses {signal.CacheMisses}");