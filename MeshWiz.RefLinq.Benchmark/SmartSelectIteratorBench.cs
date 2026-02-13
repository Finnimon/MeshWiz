using BenchmarkDotNet.Attributes;

namespace MeshWiz.RefLinq.Benchmark;

public class SmartSelectIteratorBench: RefIteratorBench<SmartSelectIterator<int,string>,string>
{
    private int[] _source=[];
    private string[] _copyTarget = [];
    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(RandomSeed);
        _source=Enumerable.Range(0,1024).Select(_=>rand.Next()).ToArray();
        _copyTarget=new string[_source.Length];
    }
    /// <inheritdoc />
    public override SmartSelectIterator<int, string> CreateIterator() => _source.AsSpan().Select(i => i.ToString());

    /// <inheritdoc />
    public override IEnumerable<string> CreateEnumerable()=>Enumerable.Select(_source,i=>i.ToString());

    /// <inheritdoc />
    protected override Span<string> GetCopyTargetSpan() => _copyTarget;
}