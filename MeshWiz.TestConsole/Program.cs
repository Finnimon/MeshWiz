using MeshWiz.Math;
using MeshWiz.Signals;

var signal=new FSignal<double,double>(double.Sin).InputGain(2000d).Cache();
var best= Signal.Analysis.BestFitBinary(signal,-1d,AABB.From(0d,6));
Console.WriteLine(best);
Console.WriteLine(signal.Cache.Count);
var best2= Signal.Analysis.BestFitNewton(signal,-1d,AABB.From(0d,6));
Console.WriteLine(best2);
Console.WriteLine(signal.Cache.Count);

var best3= Signal.Analysis.BestFitNewtonRetrying(signal,-1d,AABB.From(1,1.1),AABB.From(0d,6)
,maxTries:100);
Console.WriteLine(best3);
Console.WriteLine(signal.Cache.Count);