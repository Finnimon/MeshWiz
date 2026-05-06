using System;
using System.Numerics;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using MathNet.Spatial.Euclidean;

namespace MeshWiz.Math.Benchmark;

public class Vec3Dot
{
    private static readonly Vec3<float> Left = Vec3<float>.Create(1, 2, 3);
    private static readonly Vec3<float> Right = Vec3<float>.Create(4, 5, 6);
    private static readonly Vector3 SysLeft = Left;
    private static readonly Vector3 SysRight = Right;
    private static readonly Vec3<double> LeftD = Left.To<double>();
    private static readonly Vec3<double> RightD = Right.To<double>();
    private static readonly Vec3<Half> LeftH = Left.To<Half>();

    private static readonly Vec3<Half> RightH = Right.To<Half>();
    private readonly Consumer _consumer = new();

    //
    // [Benchmark(Baseline = true)]
    // public Vec3<float> PlusOp() => Left + Right;
    //
    //
    // [Benchmark]
    // public Vec3<double> PLusOpDouble() => LeftD + RightD;
    [Benchmark(Baseline = true), BenchmarkCategory("vec3", "float", "dot", "sys")]
    public void DotSys() =>_consumer.Consume(Vector3.Dot(SysLeft, SysRight));

    [Benchmark, BenchmarkCategory("vec3", "float", "dot")]
    public void Dot() =>
        _consumer.Consume(Vec3<float>.Dot(Left, Right));

    [Benchmark, BenchmarkCategory("vec3", "double", "dot")]
    public void DotD() =>
        _consumer.Consume(Vec3<double>.Dot(LeftD, RightD));

    [Benchmark, BenchmarkCategory("vec3", "half", "dot")]
    public void DotH() =>
        _consumer.Consume(Vec3<Half>.Dot(LeftH, RightH));
}