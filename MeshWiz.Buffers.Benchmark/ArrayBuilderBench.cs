using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using MeshWiz.RefLinq;

namespace MeshWiz.Buffers.Benchmark;

public class ArrayBuilderBench
{
    private IEnumerable<string> _refWrapper = new EnumerableWrapper<string>([]);
    private IEnumerable<nint> _valWrapper = new EnumerableWrapper<nint>([]);

    [Params(0, 1, 2, 8, 64, 4096,1_000_000,100_000_000)] public int N;

    [Params(nameof(IEnumerable<>) , nameof(Array), nameof(ICollection<>), nameof(IReadOnlyCollection<>))]
    public string Mode { get; set; } = nameof(IEnumerable<>);

    [Params(true,false)] public bool ReferenceType;

    [GlobalSetup]
    public void Setup()
    {
        var dat = Enumerable.ToArray(Enumerable.Range(0, N).Select(i=>(nint)i));
        _valWrapper = Mode switch
        {
            nameof(IEnumerable<>) => new EnumerableWrapper<nint>(dat),
            nameof(Array) => dat,
            nameof(ICollection<>) => dat.ToHashSet(),
            nameof(IReadOnlyCollection<>) => new EnumerableReadOnlyCollectionWrapper<nint>(dat),
            _ => throw new InvalidOperationException()
        };
        var dat2 = dat.Select(i => (i % 10).ToString()).ToArray();
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
    public Array EnumerableToArray() =>
        ReferenceType ? Enumerable.ToArray(_refWrapper) : Enumerable.ToArray(_valWrapper);
    
    [Benchmark]
    public Array ArrayBuilderHelper() => ReferenceType ? Iterator.ToArray(_refWrapper) : Iterator.ToArray(_valWrapper);
}