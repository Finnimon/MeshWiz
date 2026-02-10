using BenchmarkDotNet.Attributes;
using MeshWiz.RefLinq;

namespace MeshWiz.Buffers.Benchmark;

public class ArrayBuilderBench
{
    private IEnumerable<string> _wrapper=new EnumerableWrapper<string>([]);
    
    [Params(0,1/*,100,1_000_000*/)]
    public int N;

    [Params("Enumerable")] public string Mode { get; set; } = "";
    
    [GlobalSetup]
    public void Setup()
    {
        var dat= Enumerable.ToArray(Enumerable.Range(0, N).Select(i => i.ToString()));
        _wrapper=Mode.Equals("Enumerable") ? new EnumerableWrapper<string>(dat) : dat;
    }

    // [Benchmark(Baseline = true)]
    // public string[] EnumerableToArray() => Enumerable.ToArray(_wrapper);
    [Benchmark]
    public string[] BufferToArray() => Iterator.ToArray(_wrapper);
    // [Benchmark]
    // public List<string> EnumerableToList()=>Enumerable.ToList(_wrapper);
    // [Benchmark]
    // public List<string> BufferToList()=>Iterator.ToList(_wrapper);
}