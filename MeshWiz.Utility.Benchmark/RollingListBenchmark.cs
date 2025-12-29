using BenchmarkDotNet.Attributes;
using MeshWiz.Collections;
using MeshWiz.Math;

namespace MeshWiz.Utility.Benchmark;

public class RollingListBenchmark
{
    //big struct to show benefits
    private Vec3<double>[]? _prepend;
    private Vec3<double>[]? _append;

    private const int Size = 100000;

    [GlobalSetup]
    public void Setup()
    {
        _prepend = new Vec3<double>[Size];
        _append = new Vec3<double>[Size];
        var rand = new Random();
        for (int i = 0; i < Size; i++)
        {
            _prepend[i] = new Vec3<double>(rand.NextDouble(), rand.NextDouble(), rand.NextDouble());
            _append[i] = new Vec3<double>(rand.NextDouble(), rand.NextDouble(), rand.NextDouble());
        }
    }

    [Benchmark]
    public Vec3<double>[] RollingListMixedAddsToArray()
    {
        RollingList<Vec3<double>> rolyPoly = [];
        var prepend = _prepend!;
        var append = _append!;
        for (var i = 0; i < Size; i++)
        {
            rolyPoly.PushFront(prepend[i]);
            rolyPoly.PushBack(append[i]);
        }

        return rolyPoly.ToArray();
    }

    [Benchmark]
    public Vec3<double>[] LinkedListMixedAddsToArray()
    {
        LinkedList<Vec3<double>> linkedList = [];
        var prepend = _prepend!;
        var append = _append!;
        for (var i = 0; i < Size; i++)
        {
            linkedList.AddFirst(prepend[i]);
            linkedList.AddLast(append[i]);
        }

        return linkedList.ToArray();
    }
    
    [Benchmark]
    public Vec3<double>[] ListMixedAddsToArray()
    {
        List<Vec3<double>> prepList = [];
        List<Vec3<double>> appeList = [];
        var prepend = _prepend!;
        var append = _append!;
        for (var i = 0; i < Size; i++)
        {
            prepList.Add(prepend[i]);
            appeList.Add(append[i]);
        }

        prepList.Reverse();
        return [..prepList, ..appeList];
    }
}