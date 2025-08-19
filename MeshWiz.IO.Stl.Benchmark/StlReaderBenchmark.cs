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
    public IMesh<float> FastStlReaderSmallBenchmark()
        => MeshIO.ReadFile<FastStlReader,float>(StlPath("cube-binary.stl"));
    [Benchmark]
    public IMesh<float> SafeStlReaderSmallBenchmark()
        => MeshIO.ReadFile<SafeStlReader<float>,float>(StlPath("cube-binary.stl"));
    [Benchmark]
    public IMesh<float> FastStlReaderBigBenchmark()
        => MeshIO.ReadFile<FastStlReader,float>(StlPath("big-binary.stl"));
    [Benchmark]
    public IMesh<float> SafeStlReaderBigBenchmark()
        => MeshIO.ReadFile<SafeStlReader<float>,float>(StlPath("big-binary.stl"));
}

