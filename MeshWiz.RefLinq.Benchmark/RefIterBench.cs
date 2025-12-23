using BenchmarkDotNet.Attributes;

namespace MeshWiz.RefLinq.Benchmark;

[MemoryDiagnoser]
public class RefIterBench
{
    private int[] Data = [];
    private int[][] DataGrid = [];
    [Params(4, 16, 100, 1_000_000)] public int N;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random();
        Data = Enumerable.Range(0, N).Select(i => random.Next()).ToArray();
        var width = (int)double.Sqrt(N);
        DataGrid = Enumerable.Range(0, width)
            .Select(_ => Enumerable.Range(0, width).Select(i => random.Next()).ToArray()).ToArray();
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
    public int[] Linq1Realistic()
    {
        return Data.Take(Range.All).ToArray();
    }
    [Benchmark]
    public int[] RefLinq2()
    {
        SpanIterator<int> iter = Data;
        return iter.Where(i=>i>50).Select(i=>i+1).ToArray();
    }
    
    [Benchmark]
    public int[] Linq2()
    {
        return Data.Where(i=>i>50).Select(i=>i+1).ToArray();
    }
    [Benchmark]
    public int[] Linq2Realistic()
    {
        return Data.ToArray().Where(i=>i>50).Select(i=>i+1).ToArray();
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
    public int[] Linq3Realistic()
    {
        return Data.ToArray().Where(i=>i>50).Select(i=>i+1).Take(1..^25).ToArray();
    }
    [Benchmark]
    public int[] RefLinq3b()
    {
        return Data.AsSpan().Select(i=>i+1).Where(i=>i>51).Take(1..^25).ToArray();
    }
    
    
    
    [Benchmark]
    public int[] Linq3b()
    {
        return Data.Select(i=>i+1).Where(i=>i>51).Take(1..^25).ToArray();
    }
    
    [Benchmark]
    public int[] Linq3bRealistic()
    {
        return Data.ToArray().Select(i=>i+1).Where(i=>i>51).Take(1..^25).ToArray();
    }

    [Benchmark]
    public int[] RefLinq5()
    {
        SpanIterator<int> iter= Data;
        return iter.Take(100..^20).Take(..^50).ToArray();
    }
    
    [Benchmark]
    public int[] Linq5()
    {
        return Data.Take(100..^20).Take(..^50).ToArray();
    }
    [Benchmark]
    public int[] Linq5Realistic()
    {
        return Data.ToArray().Take(100..^20).Take(..^50).ToArray();
    }

    [Benchmark]
    public int[] RefLinq6SelMany()
    {
        return DataGrid.Iterate().SelectMany(x => x.Iterate()).ToArray();
    }

    [Benchmark]
    public int[] Linq6SelMany()
    {
        return DataGrid.SelectMany(x => x).ToArray();
    }

    [Benchmark]
    public int[] Linq6SelManyRealistic()
    {
        return DataGrid.ToArray().SelectMany(x => x).ToArray();
    }



    [Benchmark]
    public int[] RefLinq6WhereSelMany()
    {
        return DataGrid.Iterate().Where(i=>i[0]>0).SelectMany(x => x.Iterate()).ToArray();
    }

    [Benchmark]
    public int[] Linq6WhereSelMany()
    {
        return DataGrid.Where(i=>i[0]>0).SelectMany(x => x).ToArray();
    }    [Benchmark]
    public int[] Linq6WhereSelManyRealistic()
    {
        return DataGrid.ToArray().Where(i=>i[0]>0).SelectMany(x => x).ToArray();
    }

    
    

    [Benchmark]
    public List<int> RefLinq7SelMany()
    {
        return DataGrid.Iterate().SelectMany(x=>x.Iterate()).ToList();
    }
    
    [Benchmark]
    public List<int> Linq7SelMany()
    {
        return DataGrid.SelectMany(x => x).ToList();
    }
    [Benchmark]
    public List<int> Linq7SelManyRealistic()
    {
        return DataGrid.ToArray().SelectMany(x => x).ToList();
    }

    [Benchmark]
    public int[] RefLinq8SelFast()
    {
        return Data.Iterate().Select(x => x + 2).ToArray();
    }
    
    [Benchmark]
    public int[] Linq8SelFast()
    {
        return Data.Select(x => x + 2).ToArray();
    }
    
    [Benchmark]
    public int[] Linq8SelFastRealistic()
    {
        return Data.ToArray().Select(x => x + 2).ToArray();
    }
}