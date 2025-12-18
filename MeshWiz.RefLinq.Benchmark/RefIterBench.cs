using BenchmarkDotNet.Attributes;

namespace MeshWiz.RefLinq.Benchmark;

public class RefIterBench
{
    private int[] Data = [];

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random();
        Data= Enumerable.Range(0,1_000_000).Select(i=>random.Next()).ToArray();
        
    }
    [Benchmark]
    public int[] RefLinq1()
    {
        SpanIterator<int> iter= Data;
        return iter.Take(Range.All).ToArray();
    }

    [Benchmark]
    public int[] Linq1()
    {
        return Data.Take(Range.All).ToArray();
    }
    [Benchmark]
    public int[] RefLinq2()
    {
        SpanIterator<int> iter= Data;
        return iter.Where(i=>i>50).Select(i=>i+1).ToArray();
    }
    
    [Benchmark]
    public int[] Linq2()
    {
        return Data.Where(i=>i>50).Select(i=>i+1).ToArray();
    }

    [Benchmark]
    public int[] RefLinq3()
    {
        SpanIterator<int> iter= Data;
        return iter.Where(i=>i>50).Select(i=>i+1).Take(1..^25).ToArray();
    }
    
    

    [Benchmark]
    public int[] Linq3()
    {
        return Data.Where(i=>i>50).Select(i=>i+1).Take(1..^25).ToArray();
    }
    
    [Benchmark]
    public int[] RefLinq5()
    {
        SpanIterator<int> iter= Data;
        return iter.Take(100..^20).ToArray();
    }

    [Benchmark]
    public int[] Linq5()
    {
        return Data.Take(100..^20).ToArray();
    }
}