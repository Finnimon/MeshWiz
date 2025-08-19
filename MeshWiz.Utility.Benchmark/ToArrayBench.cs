using System.Numerics;
using BenchmarkDotNet.Attributes;
using MeshWiz.Math;

namespace MeshWiz.Utility.Benchmark;

public class ArrayCopyBench
{
    [Params(0, 1, 1000, 100000)] public int ArraySize;
    private Vector3<float>[]? _array;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _array = new Vector3<float>[ArraySize];
        var rand = new Random();
        for (var i = 0; i < _array.Length; i++)
            _array[i] = new(rand.NextSingle(), rand.NextSingle(), rand.NextSingle());
    }

    private Vector3<float>[] ArrayNotNull() => _array!;


    [Benchmark(Baseline = true)]
    public Vector3<float>[] ToArray() => ArrayNotNull().ToArray();

    [Benchmark]
    public Vector3<float>[] Range() => ArrayNotNull()[..];

    [Benchmark]
    public Vector3<float>[] Copy()
    {
        var arr = ArrayNotNull();
        var copy = new Vector3<float>[arr.Length];
        arr.CopyTo(copy, 0);
        
        return copy;
    }
}