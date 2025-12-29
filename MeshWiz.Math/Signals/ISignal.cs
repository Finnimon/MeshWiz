using System.Collections.Concurrent;
using System.Numerics;

namespace MeshWiz.Math.Signals;

public interface ISignal<in TIn, out TOut>
    where TIn : unmanaged, IFloatingPointIeee754<TIn>
    where TOut : unmanaged, IFloatingPointIeee754<TOut>
{
    TOut Sample(TIn input);
    Func<TIn, TOut> AsFunc() => Sample;
}

public interface IAsyncSignal<in TIn, TOut> : ISignal<TIn, TOut>
    where TIn : unmanaged, IFloatingPointIeee754<TIn>
    where TOut : unmanaged, IFloatingPointIeee754<TOut>
{
    Task<TOut> SampleAsync(TIn input, CancellationToken token = default) => Task.Run(() => Sample(input), token);
}

public sealed record ConcurrentCachingSignal<TIn, TOut>(ISignal<TIn, TOut> Underlying) : IAsyncSignal<TIn, TOut>
    where TOut : unmanaged, IFloatingPointIeee754<TOut>
    where TIn : unmanaged, IFloatingPointIeee754<TIn>
{
    private readonly ConcurrentDictionary<TIn, TOut> _cache = [];
    /// <inheritdoc />
    public TOut Sample(TIn input) => _cache.GetOrAdd(input, Underlying.Sample);

    /// <inheritdoc />
    public Task<TOut> SampleAsync(TIn input, CancellationToken token = default) 
        => Underlying is IAsyncSignal<TIn,TOut> asyncSource
            ? GetOrAddAsync(asyncSource,_cache,input,token) 
            : Task.Run(() => Sample(input), token);

    private static async Task<TOut> GetOrAddAsync(IAsyncSignal<TIn, TOut> asyncSource, ConcurrentDictionary<TIn, TOut> cache, TIn input, CancellationToken token)
    {
        if (cache.TryGetValue(input, out var v))
            return v;
        var newResult =await asyncSource.SampleAsync(input, token);
        cache[input]=newResult;
        return newResult;
    }
}