using System.Numerics;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Iterator = MeshWiz.RefLinq.Iterator;

namespace MeshWiz.RefLinq.Benchmark;

public class SumBench
{
    [Params(0, 1, 64, 128, 1_000_000)] public int N;
    public const int Seed = 821348723;
    public int[] IntData = [];
    public Half[] ShortData = [];

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(Seed);
        IntData = Iterator.Range(0, N).Select(_ => rand.Next()).ToArray();
        rand = new Random(Seed);
        ShortData = IntData.Select(_=>rand.NextSingle()).Select(Half.CreateTruncating).ToArray();
    }

    //
    // [Benchmark]
    // public int LinqInt() => Enumerable.Sum(IntData);
    // [Benchmark]
    // public float LinqFloat() => Enumerable.Sum(FloatData);
    //
    // [Benchmark]
    // public int RefLinqInt() => Iterator.Sum(IntData);
    // [Benchmark]
    // public float RefLinqFloat() => Iterator.Sum(FloatData);
    [Benchmark]
    public double RefLinq() => ShortData.Select(f => (double)f).Sum(0.0);

    [Benchmark]
    public double Linq() => Enumerable.Select(ShortData, f => (double)f).Sum();
}