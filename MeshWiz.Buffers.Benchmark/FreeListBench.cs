using System.Buffers;
using BenchmarkDotNet.Attributes;

namespace MeshWiz.Buffers.Benchmark;

[MemoryDiagnoser]
[ThreadingDiagnoser] // optional but useful
[DisassemblyDiagnoser(maxDepth: 2)] // later, when optimizing
public class AllocatorBench<T>
    where T: unmanaged
{
    public const int LinearRentStep = 1_000, MaxLinearRentSize = 128;
    private Freelist _allocator = new Freelist(MaxLinearRentSize, false);

    [GlobalCleanup]
    public void GlobalSetup()
    {
        // _allocator = new Freelist(Allocator.InitialSharedCapacity, false);
    }

    //
    [Benchmark(Baseline = true)]
    public void LinearArrayPool()
    {
        var rent = ArrayPool<T>.Shared.Rent(MaxLinearRentSize);
        ArrayPool<T>.Shared.Return(rent);
    }
    // //
    // [Benchmark]
    // public void LinearPool()
    // {
    //     using var rent = Pool.Rent<T>(MaxLinearRentSize);
    // }
    //
    [Benchmark]
    public void Freelist()
    {
        using var rent = Buffers.Freelist.Shared.Rent<T>(MaxLinearRentSize);
    }

    // [Benchmark]
    // public void FreelistGrow()
    // {
    //     using var rent = _allocator.Rent<T>(MaxLinearRentSize-500);
    //     var initital = rent.Span.Length;
    //     Buffers.Freelist.GrowGreedy(in rent);
    // }
    //
    // [Benchmark]
    // public void FreelistTryGrow()
    // {
    //     using var rent = _allocator.Rent<T>(MaxLinearRentSize-500);
    //     Buffers.Freelist.TryGrow(in rent,20);
    // }
    //
    //
    // [Benchmark]
    // public void SharedFreelist()
    // {
    //     using var rent = Buffers.Freelist.Shared.Rent<T>(MaxLinearRentSize);
    //     // using var rent2=Buffers.Freelist.Shared.Rent<T>(MaxLinearRentSize);
    // }
}