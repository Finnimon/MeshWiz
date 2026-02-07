using System.Collections;
using BenchmarkDotNet.Attributes;
using MeshWiz.RefLinq;

namespace MeshWiz.Buffers.Benchmark;

public class EnumerableWrapper<T> : IEnumerable<T>
{
    private readonly T[] _data;

    public EnumerableWrapper(T[] data)
    {
        _data = data;
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => new AdapterIterator<T>(_data);

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}


public class ArrayBuilderBench
{
    private EnumerableWrapper<string> _wrapper=new([]);

    [Params(0,1,4,8,16,17,100,1_000_000)]
    public int N;
    [GlobalSetup]
    public void Setup()
    {
        
        var dat= Enumerable.Range(0, N).Select(i => i.ToString()).ToArray();
        _wrapper = new EnumerableWrapper<string>(dat);
    }

    [Benchmark(Baseline = true)]
    public string[] EnumerableToArray() => Enumerable.ToArray(_wrapper);

    [Benchmark]
    public string[] BufferToArray()
    {
        using BufferedArrayBuilder<string> b = new();
        b.AddEnumeratingInlined(_wrapper);
        return b.ToArray();
    }
}