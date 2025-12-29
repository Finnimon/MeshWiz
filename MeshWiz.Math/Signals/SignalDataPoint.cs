using System.Numerics;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math.Signals;

public readonly record struct SignalDataPoint<TIn, TOut>(
    TIn Input,
    TOut OutPut)
    where TIn : unmanaged, IFloatingPointIeee754<TIn>
    where TOut : unmanaged, IFloatingPointIeee754<TOut>
{
    public static SignalDataPoint<TIn, TOut> Create<TSignal>(TSignal signal,
        TIn input)
        where TSignal : ISignal<TIn, TOut> =>
        new(input,
            signal.Sample(input));

    public SignalDataPoint<TIn, TOut> Shift(TOut value) => this with { OutPut = OutPut + value };

    public static SignalDataPoint<TIn, TOut> Min(params ReadOnlySpan<SignalDataPoint<TIn, TOut>> opts)
    {
        if (opts.IsEmpty)
            ThrowHelper.ThrowArgumentException(nameof(opts));
        var min = opts[0];
        for (var i = 1; i < opts.Length; i++)
            min = min.OutPut < opts[i].OutPut ? min : opts[i];
        return min;
    }

    public static SignalDataPoint<TIn, TOut> Closest(TOut target, params ReadOnlySpan<SignalDataPoint<TIn, TOut>> opts)
    {
        if (opts.IsEmpty)
            ThrowHelper.ThrowArgumentException(nameof(opts));
        var min = 0;
        var minDist = TOut.Abs(opts[0].OutPut - target);
        for (var i = 1; i < opts.Length; i++)
        {
            var d = TOut.Abs(opts[i].OutPut - target);
            if(minDist<d)
                continue;
            min = i;
            minDist = d;
        }

        return opts[min];
    }

    public static SignalDataPoint<TIn, TOut> Closest(TOut target, SignalDataPoint<TIn, TOut> a, SignalDataPoint<TIn, TOut> b) =>
        TOut.Abs(a.OutPut - target) < TOut.Abs(b.OutPut - target)
            ? a
            : b;

    public bool IsAcceptable(TOut target, TOut eps = default) =>
        OutPut.IsApprox(target, eps == default ? Numbers<TOut>.ZeroEpsilon : eps);

    public static SignalDataPoint<TIn, TOut> Max(params ReadOnlySpan<SignalDataPoint<TIn, TOut>> opts)
    {
        if (opts.IsEmpty)
            ThrowHelper.ThrowArgumentException(nameof(opts));
        var max = opts[0];
        for (var i = 1; i < opts.Length; i++)
            max = max.OutPut > opts[i].OutPut ? max : opts[i];
        return max;
    }
}