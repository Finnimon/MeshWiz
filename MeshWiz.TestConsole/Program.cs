

using MeshWiz.Math;
using MeshWiz.Math.Benchmark;
using MeshWiz.RefLinq;

Console.WriteLine("");
int[] intArr=[0,1,2,3,4,5,6];
var span=intArr.AsSpan();
SpanIterator<int> spanIter=span;
foreach (var i in spanIter.Where(num=>num>2).Select(i=>i+1).Skip(1))
{
    Console.WriteLine(i);
}