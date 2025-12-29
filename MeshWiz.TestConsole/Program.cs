using MeshWiz.Collections;
using MeshWiz.Math;
using MeshWiz.Math.Benchmark;
using MeshWiz.Math.Signals;

// var signal = new FSignal<double, double>(double.Sin).InputGain(1000).Cache();
var b=new GeodesicBench<double>();
b.Setup();
for(var i=0;i<100;i++)
{
    var per = b.TracePeriod();
    var poses = per.FinalizedPoses;
    Console.WriteLine($"{i} {poses.HasValue} {poses.Value.Count}");
}
// b.ExitNewton()
// RollingSpan<int> roll=(int[])[0,1,2,3,4];
// Console.WriteLine(roll[-1]);