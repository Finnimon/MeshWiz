using BenchmarkDotNet.Attributes;

namespace MeshWiz.Utility.Benchmark;

public class OnceBench
{
    [Params(0,1,10,1000000)] public int Size;
    private int[] _testData=[];

    [GlobalSetup]
    public void Setup() => _testData = Enumerable.Range(0, Size).Select(_ => new Random().Next()).ToArray();


    [Benchmark]
    public int CreateSum()
    {
        var once = Bool.Once();
        var sum = 0;
        foreach (var i in _testData)
        {
            sum += once ? i * 2 : i;
        }

        return sum;
    }

    
    [Benchmark]
    public int CTorSum()
    {
        Once once = new();
        var sum = 0;
        foreach (var i in _testData)
        {
            sum += once ? i * 2 : i;
        }

        return sum;
    }

    
}