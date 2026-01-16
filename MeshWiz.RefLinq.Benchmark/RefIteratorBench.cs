using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;

namespace MeshWiz.RefLinq.Benchmark;

public abstract class RefIteratorBench<TIter, TItem>
    where TIter : IRefIterator<TIter, TItem>, allows ref struct
{
    [Benchmark]
    public abstract TIter CreateIterator();

    protected abstract Span<TItem> GetCopyTargetSpan();
    public TIter CreateIteratorTimer() => CreateIterator();

    [Benchmark]
    public TItem First() => CreateIterator().First();

    [Benchmark]
    public TItem? FirstOrDefault() => CreateIterator().FirstOrDefault();

    [Benchmark]
    public bool TryGetFirst() => CreateIterator().TryGetFirst(out _);

    [Benchmark]
    public TItem Last() => CreateIterator().Last();

    [Benchmark]
    public TItem? LastOrDefault() => CreateIterator().LastOrDefault();

    [Benchmark]
    public bool TryGetLast() => CreateIterator().TryGetLast(out _);

    [Benchmark]
    public int Count() => CreateIterator().Count();

    [Benchmark]
    public bool TryGetNonEnumeratedCount() => CreateIterator().TryGetNonEnumeratedCount(out _);

    [Benchmark]
    public void CopyTo() => CreateIterator().CopyTo(GetCopyTargetSpan());

    [Benchmark]
    public TItem[] ToArray() => CreateIterator().ToArray();

    [Benchmark]
    public List<TItem> ToList() => CreateIterator().ToList();

    [Benchmark]
    public bool Any() => CreateIterator().Any();

    [Benchmark]
    public int EstimateCount()=>CreateIterator().EstimateCount();

    [Benchmark]
    public TItem Min()=>CreateIterator().Min();
    [Benchmark]
    public TItem Max()=>CreateIterator().Max();
    [Benchmark]
    public TItem? MinOrDefault()=>CreateIterator().MinOrDefault();
    [Benchmark]
    public TItem? MaxOrDefault()=>CreateIterator().MaxOrDefault();

    public const int RandomSeed=1093274091;
}

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
    protected override Span<string> GetCopyTargetSpan() => _copyTarget;
}
