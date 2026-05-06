using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.RefLinq;
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

    public static SignalDataPoint<TIn, TOut> Closest(TOut target, params ReadOnlySpan<SignalDataPoint<TIn, TOut>> opts) =>
        opts.Length switch
        {
            0 => ThrowHelper.ThrowArgumentException<SignalDataPoint<TIn, TOut>>(nameof(opts)),
            1 => opts[0],
            2 => Closest(target, opts[0], opts[1]),
            3 => Closest(target, opts[0], opts[1], opts[2]),
            _ => ClosestIterative(target, opts)
        };

    private static SignalDataPoint<TIn, TOut> ClosestIterative(TOut target,
        ReadOnlySpan<SignalDataPoint<TIn, TOut>> opts) =>
        opts.Iterate().MinBy(v => TOut.Abs(target - v.OutPut));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SignalDataPoint<TIn, TOut> Closest(TOut target, SignalDataPoint<TIn, TOut> a,
        SignalDataPoint<TIn, TOut> b) =>
        TOut.Abs(a.OutPut - target) < TOut.Abs(b.OutPut - target)
            ? a
            : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SignalDataPoint<TIn, TOut> Closest(TOut target, SignalDataPoint<TIn, TOut> a,
        SignalDataPoint<TIn, TOut> b, SignalDataPoint<TIn, TOut> c)
        => Closest(target, Closest(target, a, b), b);

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