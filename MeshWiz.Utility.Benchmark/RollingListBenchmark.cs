using BenchmarkDotNet.Attributes;
using MeshWiz.Math;

namespace MeshWiz.Utility.Benchmark;

public class RollingListBenchmark
{
    //big struct to show benefits
    private Vector3<double>[]? _prepend;
    private Vector3<double>[]? _append;

    private const int Size = 100000;

    [GlobalSetup]
    public void Setup()
    {
        _prepend = new Vector3<double>[Size];
        _append = new Vector3<double>[Size];
        var rand = new Random();
        for (int i = 0; i < Size; i++)
        {
            _prepend[i] = new Vector3<double>(rand.NextDouble(), rand.NextDouble(), rand.NextDouble());
            _append[i] = new Vector3<double>(rand.NextDouble(), rand.NextDouble(), rand.NextDouble());
        }
    }

    [Benchmark]
    public Vector3<double>[] RollingListMixedAddsToArray()
    {
        RollingList<Vector3<double>> rolyPoly = [];
        var prepend = _prepend!;
        var append = _append!;
        for (var i = 0; i < Size; i++)
        {
            rolyPoly.PushFront(prepend[i]);
            rolyPoly.PushBack(append[i]);
        }

        return rolyPoly.ToArrayFast();
    }

    [Benchmark]
    public Vector3<double>[] LinkedListMixedAddsToArray()
    {
        LinkedList<Vector3<double>> linkedList = [];
        var prepend = _prepend!;
        var append = _append!;
        for (var i = 0; i < Size; i++)
        {
            linkedList.AddFirst(prepend[i]);
            linkedList.AddLast(append[i]);
        }

        return linkedList.ToArray();
    }
}