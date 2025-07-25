using System.Numerics;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace MeshWiz.Math.Benchmark;

public class ForeachOnBigStructBench<TNum>
where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    private Vector4<TNum>[]? _vectors;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random();
        _vectors = new Vector4<TNum>[10000];
        for (var i = 0; i < _vectors.Length; i++) 
            _vectors[i] = Vector4<double>.FromXYZW(
                    rand.NextDouble(), 
                    rand.NextDouble(), 
                    rand.NextDouble(), 
                    rand.NextDouble())
                .To<TNum>();
    }
    [Benchmark]
    public Vector4<TNum> Aggregate()
        => _vectors!.Where(v=>v.Length>TNum.One).Aggregate((v1, v2) => v1 + v2);

    [Benchmark]
    public Vector4<TNum> ForI()
    {
        var vecs = _vectors!;
        var sum=Vector4<TNum>.Zero;
        for(var i=0;i<vecs.Length;i++)
        {
            var vec = vecs[i];
            if(vec.Length<TNum.One) continue;
            sum += vec;
        }
        return sum;
    }

    [Benchmark]
    public Vector4<TNum> ForEach()
    =>ForEachHelper(_vectors!);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Vector4<TNum> ForEachHelper(IEnumerable<Vector4<TNum>> enumerator)
    {
        var sum=Vector4<TNum>.Zero;
        foreach (var vec in enumerator) 
        {
            if(vec.Length<TNum.One) continue;
            sum += vec;
        }
        return sum;
    }
}