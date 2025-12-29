using System.Numerics;

namespace MeshWiz.Math.Signals;

public sealed record CachingSignal<TIn, TOut>(ISignal<TIn, TOut> Signal)
    : ISignal<TIn, TOut>
    where TIn : unmanaged, IFloatingPointIeee754<TIn>
    where TOut : unmanaged, IFloatingPointIeee754<TOut>
{
    private readonly Dictionary<TIn, TOut> _cache = [];
    public IReadOnlyDictionary<TIn, TOut> Cache => _cache;
    public int CacheHits { get; private set; } = 0;
    public int CacheMisses { get; private set; } = 0;
    public TOut Sample(TIn input)
    {
        if (_cache.TryGetValue(input, out var value))
        {
            CacheHits++;
            return value;
        }

        CacheMisses++;
        var result = Signal.Sample(input);
        _cache.Add(input, result);
        return result;
    }
}