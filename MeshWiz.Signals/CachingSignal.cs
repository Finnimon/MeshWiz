using System.Numerics;

namespace MeshWiz.Signals;

public sealed record CachingSignal<TIn, TOut>(ISignal<TIn, TOut> Signal)
    : ISignal<TIn, TOut>
    where TIn : unmanaged, IFloatingPointIeee754<TIn>
    where TOut : unmanaged, IFloatingPointIeee754<TOut>
{
    private readonly Dictionary<TIn, TOut> _cache = [];
    public IReadOnlyDictionary<TIn, TOut> Cache => _cache;

    public TOut Sample(TIn input)
    {
        if (_cache.TryGetValue(input, out var value))
            return value;
        var result = Signal.Sample(input);
        _cache.Add(input, result);
        return result;
    }
}