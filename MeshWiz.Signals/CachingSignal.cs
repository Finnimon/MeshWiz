using System.Numerics;

namespace MeshWiz.Signals;

public sealed record CachingSignal<TSignal, TIn, TOut>(TSignal Signal)
    : ISignal<TIn, TOut>
    where TSignal : ISignal<TIn, TOut>
    where TIn : unmanaged, IFloatingPointIeee754<TIn>
    where TOut : unmanaged, IFloatingPointIeee754<TOut>
{
    private readonly Dictionary<TIn, TOut> _dictionary = [];

    public TOut Sample(TIn input)
    {
        if (_dictionary.TryGetValue(input, out var value))
            return value;
        var result = Signal.Sample(input);
        _dictionary.Add(input, result);
        return result;
    }
}