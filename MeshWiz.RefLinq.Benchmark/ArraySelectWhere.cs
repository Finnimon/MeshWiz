using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace MeshWiz.RefLinq.Benchmark;

public class ArraySelectWhere
{
    [Params(0,1,100,1000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        Random rand = new(RefIteratorBench<Iterator<int>, int>.RandomSeed); 
        _array = Enumerable.Range(0, N).Select(_=>rand.Next()).ToArray();
    }
    
    private int[] _array=[];
    
    [Benchmark]
    public int[] RefLinqToArr() => GetRefLinqIter().ToArray();
    [Benchmark]
    public int RefLinqFirst()=>GetRefLinqIter().FirstOrDefault();
    
    [Benchmark]
    public int RefLinqLast()=>GetRefLinqIter().LastOrDefault();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SmartSelectWhereIterator<int, int> GetRefLinqIter() => _array.AsSpan().Select(x => x + 1).Where(i => i % 2 == 0);

    [Benchmark(Baseline = true)]
    public int[] LinqToArr() => GetEnumerable().ToArray();
    
    [Benchmark]
    public int LinqFirst() => GetEnumerable().FirstOrDefault();
    [Benchmark]
    public int LinqLast() => GetEnumerable().LastOrDefault();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IEnumerable<int> GetEnumerable() => Enumerable.Select(_array,x => x + 1).Where(i => i % 2 == 0);
}