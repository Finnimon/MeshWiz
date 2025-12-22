using System.Numerics;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.Signals;

public readonly record struct SignalResult<TIn, TOut>(
    TIn Input,
    TOut Result)
    where TIn : unmanaged, IFloatingPointIeee754<TIn>
    where TOut : unmanaged, IFloatingPointIeee754<TOut>
{
    public static SignalResult<TIn, TOut> Create<TSignal>(TSignal signal,
        TIn input)
        where TSignal : ISignal<TIn, TOut> =>
        new(input,
            signal.Sample(input));

    public SignalResult<TIn, TOut> Shift(TOut value) => this with { Result = Result + value };

    public static SignalResult<TIn, TOut> Min(params ReadOnlySpan<SignalResult<TIn, TOut>> opts)
    {
        if (opts.IsEmpty)
            ThrowHelper.ThrowArgumentException(nameof(opts));
        var min = opts[0];
        for (var i = 1; i < opts.Length; i++)
            min = min.Result < opts[i].Result ? min : opts[i];
        return min;
    }

    public static SignalResult<TIn, TOut> Max(params ReadOnlySpan<SignalResult<TIn, TOut>> opts)
    {
        if (opts.IsEmpty)
            ThrowHelper.ThrowArgumentException(nameof(opts));
        var max = opts[0];
        for (var i = 1; i < opts.Length; i++)
            max = max.Result > opts[i].Result ? max : opts[i];
        return max;
    }
}