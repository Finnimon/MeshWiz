using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;

namespace MeshWiz.RefLinq.Benchmark;

public abstract class RefIteratorBench<TIter, TItem>
    where TIter : IRefIterator<TIter, TItem>, allows ref struct
{
    [Benchmark]
    public abstract TIter CreateIterator();
    [Benchmark]
    public abstract IEnumerable<TItem> CreateEnumerable();
    
    protected abstract Span<TItem> GetCopyTargetSpan();

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