using BenchmarkDotNet.Attributes;

namespace MeshWiz.Math.Benchmark;


public class Vec3Length
{
    private static Vec3<double> DblVec() => Vec3<double>.Create(1, 0.5, -1);
    private static Vec3<float> FloatVec() => Vec3<float>.Create(1, 0.5f, -1);
    [Benchmark]
    public double GetLengthDouble() => Vec3<double>.GetLength(DblVec());
    [Benchmark]
    public double GetLengthFloat() => Vec3<float>.GetLength(FloatVec());
}