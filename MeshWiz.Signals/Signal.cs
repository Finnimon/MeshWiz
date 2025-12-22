using System.Numerics;
using MeshWiz.Math;

namespace MeshWiz.Signals;

public static partial class Signal
{
    public static ClampedSignal<TIn, TOut> Clamped<TIn, TOut>(this ISignal<TIn,TOut> sig,
        AABB<TOut> clamp)
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => new(sig, clamp);
    public static CachingSignal<TIn, TOut> Cache<TIn, TOut>(this ISignal<TIn,TOut> sig)
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => new(sig);

    public static Gain<TIn, TOut> Gain<TIn, TOut>(this ISignal<TIn,TOut> sig,
        TOut gain)
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => new(sig, gain);
    public static Shift<TIn, TOut> Shift<TIn, TOut>(this ISignal<TIn,TOut> sig,
        TOut shift)
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => new(sig, shift);
    public static SignalResult<TIn, TOut> GetResult<TSig, TIn, TOut>(this TSig sig,
        TIn input)
        where TSig : ISignal<TIn, TOut>
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => SignalResult<TIn, TOut>.Create(sig, input);

}