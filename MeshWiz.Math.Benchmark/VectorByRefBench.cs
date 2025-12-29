using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace MeshWiz.Math.Benchmark;

public class VectorByRefBench<TNum> where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    private Vec3<TNum>[]? _vectors;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _vectors=new  Vec3<TNum>[20];
        var rand=new Random();
        for (int i = 0; i < _vectors.Length; i++)
            _vectors[i] = new(
                TNum.CreateTruncating(rand.NextDouble()),
                TNum.CreateTruncating(rand.NextDouble()),
                TNum.CreateTruncating(rand.NextDouble())
            );
    }

    [Benchmark]
    public Vec3<TNum> AddRef()
    {
        var vec=Vec3<TNum>.Zero;
        for (int i = 0; i < _vectors!.Length; i++)
            vec = vec + _vectors[i];
        return vec;
    }
    
    
}