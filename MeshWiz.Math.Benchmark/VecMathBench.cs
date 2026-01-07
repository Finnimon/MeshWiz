using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace MeshWiz.Math.Benchmark;

public class VecMathBench
{
    public Vec4<float> A;
    public Vec4<float> B;
    public Vector4 SysA, SysB;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random();
        var comp= Enumerable.Range(0, 8).Select(_ => rand.NextSingle()).Select(float.CreateTruncating).ToArray();
        A=Vec4<float>.FromComponents(comp[0..4]);
        B=Vec4<float>.FromComponents(comp[4..8]);
        var flA = A.To<float>();
        var flB = B.To<float>();
        SysA=new(flA.X,flA.Y,flA.Z,flA.W);
        SysB=new(flB.X,flB.Y,flB.Z,flB.W);
    }

    [Benchmark(Baseline = true)]
    public Vector4 SysMul() => SysA * SysB;
    // [Benchmark]
    // public Vec4<float> MulOld()
    // {
    //     return A * B;
    // }

    // public Vec4<float> MulOld()
    // {
    //     return A * B;
    // }
    [Benchmark]
    public Vec4<float> VecMul()
    {
        return Vec<float>.Mul(A, B);
    }
}