using BenchmarkDotNet.Attributes;

namespace MeshWiz.RefLinq.Benchmark;

public class RefIterBench
{
    private int[] Data=Enumerable.Range(0,100).ToArray();

    [Benchmark]
    public int[] RefLinq1()
    {
        SpanIterator<int> iter= Data;
        return iter.Take(Range.All).ToArray();
    }

    [Benchmark]
    public int[] RefLinq2()
    {
        SpanIterator<int> iter= Data;
        return iter.Where(i=>i>50).Select(i=>i+1).ToArray();
    }
    
    [Benchmark]
    public int[] RefLinq3()
    {
        SpanIterator<int> iter= Data;
        return iter.Where(i=>i>50).Select(i=>i+1).Take(1..25).ToArray();
    }
    
    
    [Benchmark]
    public int[] Linq1()
    {
        return Data.Take(Range.All).ToArray();
    }

    [Benchmark]
    public int[] Linq2()
    {
        return Data.Where(i=>i>50).Select(i=>i+1).ToArray();
    }
    
    [Benchmark]
    public int[] Linq3()
    {
        return Data.Where(i=>i>50).Select(i=>i+1).Take(1..25).ToArray();
    }
}