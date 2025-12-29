using System.Numerics;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace MeshWiz.Math.Benchmark;

public class ForeachOnBigStructBench<TNum>
where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    private Vec4<TNum>[]? _vectors;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random();
        _vectors = new Vec4<TNum>[10000];
        for (var i = 0; i < _vectors.Length; i++) 
            _vectors[i] = Vec4<double>.Create(
                    rand.NextDouble(), 
                    rand.NextDouble(), 
                    rand.NextDouble(), 
                    rand.NextDouble())
                .To<TNum>();
    }
    [Benchmark]
    public Vec4<TNum> Aggregate()
        => _vectors!.Where(v=>v.Length>TNum.One).Aggregate((v1, v2) => v1 + v2);

    [Benchmark]
    public Vec4<TNum> ForI()
    {
        var vecs = _vectors!;
        var sum=Vec4<TNum>.Zero;
        for(var i=0;i<vecs.Length;i++)
        {
            var vec = vecs[i];
            if(vec.Length<TNum.One) continue;
            sum += vec;
        }
        return sum;
    }

    [Benchmark]
    public Vec4<TNum> ForEach()
    =>ForEachHelper(_vectors!);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Vec4<TNum> ForEachHelper(IEnumerable<Vec4<TNum>> enumerator)
    {
        var sum=Vec4<TNum>.Zero;
        foreach (var vec in enumerator) 
        {
            if(vec.Length<TNum.One) continue;
            sum += vec;
        }
        return sum;
    }
}