using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using MeshWiz.RefLinq;

namespace MeshWiz.CompilerServices.Benchmark;

public class InlineArrayBench
{
    [Benchmark(Baseline = true)]
    public int Sum()
    {
        InlineArray16<int> arr = default;

        arr[0] = 1;
        arr[1] = 2;
        arr[14] = 3;
        var sum = 0;
        foreach (var i in arr)
        {
            sum += i;
        }

        return sum;
    }

    [Benchmark]
    public int RefSum()
    {
        InlineRefArray16<int> arr = default;
        arr[0] = 1;
        arr[1] = 2;
        arr[14] = 3;
        var sum = 0;
        foreach (var i in arr)
        {
            sum += i;
        }

        return sum;
    }

    [Benchmark]
    public int FunctionalTest()
    {
        InlineArray4<int> arr = default;
        arr[0] = 2;
        arr[1] = 3;
        arr[3] = 4;
        InlineRefArray16<ReadOnlySpan<int>> spans = default;
        spans[0] = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<InlineArray4<int>,int>(ref arr),4);
        for (var i = 1; i < spans.Length; i++) spans[i] = spans[0];
        var sum = 0;
        foreach (var readOnlySpan in spans)
        {
            sum+=readOnlySpan.Sum();
        }

        return sum;
    }
}