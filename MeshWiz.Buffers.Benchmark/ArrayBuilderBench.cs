using BenchmarkDotNet.Attributes;
using MeshWiz.RefLinq;

namespace MeshWiz.Buffers.Benchmark;

public class ArrayBuilderBench
{
    private IEnumerable<string> _refWrapper = new EnumerableWrapper<string>([]);
    private IEnumerable<int> _valWrapper = new EnumerableWrapper<int>([]);

    [Params(/*0, 1, */2/*, 8, 17, 4096*/)]
    public int N;

    [Params(nameof(IEnumerable<>)/*, nameof(Array), nameof(ICollection<>), nameof(IReadOnlyCollection<>)*/)]
    public string Mode { get; set; } = "";

    [Params(false)] public bool ReferenceType;

    [GlobalSetup]
    public void Setup()
    {
        var dat = Enumerable.ToArray(Enumerable.Range(0, N));
        _valWrapper = Mode switch
        {
            nameof(IEnumerable<>) => new EnumerableWrapper<int>(dat),
            nameof(Array) => dat,
            nameof(ICollection<>) => dat.ToHashSet(),
            nameof(IReadOnlyCollection<>) => new EnumerableReadOnlyCollectionWrapper<int>(dat),
            _ => throw new InvalidOperationException()
        };
        var dat2 = dat.Select(i => i.ToString()).ToArray();
        _refWrapper = Mode switch
        {
            nameof(IEnumerable<>) => new EnumerableWrapper<string>(dat2),
            nameof(Array) => dat2,
            nameof(ICollection<>) => dat2.ToHashSet(),
            nameof(IReadOnlyCollection<>) => new EnumerableReadOnlyCollectionWrapper<string>(dat2),
            _ => throw new InvalidOperationException()
        };
    }

    [Benchmark(Baseline = true)]
    public object EnumerableToArray() =>
        ReferenceType ? Enumerable.ToArray(_refWrapper) : Enumerable.ToArray(_valWrapper);

    [Benchmark]
    public object BufferToArray() => ReferenceType ? Iterator.ToArray(_refWrapper) : Iterator.ToArray(_valWrapper);
    // [Benchmark]
    // public List<string> EnumerableToList()=>Enumerable.ToList(_wrapper);
    // [Benchmark]
    // public List<string> BufferToList()=>Iterator.ToList(_wrapper);
}