using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace MeshWiz.Math.Benchmark;

public class VectorByRefBench<TNum> where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    private Vector3<TNum>[]? _vectors;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _vectors=new  Vector3<TNum>[20];
        var rand=new Random();
        for (int i = 0; i < _vectors.Length; i++)
            _vectors[i] = new(
                TNum.CreateTruncating(rand.NextDouble()),
                TNum.CreateTruncating(rand.NextDouble()),
                TNum.CreateTruncating(rand.NextDouble())
            );
    }

    [Benchmark]
    public Vector3<TNum> AddRef()
    {
        var vec=Vector3<TNum>.Zero;
        for (int i = 0; i < _vectors!.Length; i++)
            vec = vec + _vectors[i];
        return vec;
    }
    
    
}