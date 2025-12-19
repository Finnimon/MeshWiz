

using MeshWiz.RefLinq;
using MeshWiz.RefLinq.Benchmark;

var refIterBench = new RefIterBench();
refIterBench.Setup();
for(var i=0;i<1000;i++)
    refIterBench.RefLinq6SelMany();