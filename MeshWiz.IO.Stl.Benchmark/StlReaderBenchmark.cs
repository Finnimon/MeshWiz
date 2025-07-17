using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using MeshWiz.Math;

namespace MeshWiz.IO.Stl.Benchmark;

public class StlReaderBenchmark
{
    public static string StlPath(string name)
    {
        var basePath = Path.GetDirectoryName(WhereAmI())!;
        return Path.Combine(basePath, "Assets",name);
    }

    public static string WhereAmI([CallerFilePath] string callerFilePath = "") => callerFilePath;

    [Benchmark]
    public Mesh3<float> FastStlReaderSmallBenchmark()
        => IMeshReader<float>.ReadFile<FastStlReader>(StlPath("cube-binary.stl"));
    [Benchmark]
    public Mesh3<float> SafeStlReaderSmallBenchmark()
        => IMeshReader<float>.ReadFile<SafeStlReader<float>>(StlPath("cube-binary.stl"));
    [Benchmark]
    public Mesh3<float> FastStlReaderBigBenchmark()
        => IMeshReader<float>.ReadFile<FastStlReader>(StlPath("big-binary.stl"));
    [Benchmark]
    public Mesh3<float> SafeStlReaderBigBenchmark()
        => IMeshReader<float>.ReadFile<SafeStlReader<float>>(StlPath("big-binary.stl"));
}

