using System.Numerics;
using MeshWiz.Math;

namespace MeshWiz.Signals;

public static partial class Signal
{
    public static ClampedSignal<TSig, TIn, TOut> Clamped<TSig, TIn, TOut>(this TSig sig,
        AABB<TOut> clamp)
        where TSig : ISignal<TIn, TOut>
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => new(sig, clamp);
    public static CachingSignal<TSig, TIn, TOut> Cache<TSig, TIn, TOut>(this TSig sig)
        where TSig : ISignal<TIn, TOut>
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => new(sig);

    public static Gain<TSig, TIn, TOut> Gain<TSig, TIn, TOut>(this TSig sig,
        TOut gain)
        where TSig : ISignal<TIn, TOut>
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => new(sig, gain);
    public static Shift<TSig, TIn, TOut> Shift<TSig, TIn, TOut>(this TSig sig,
        TOut shift)
        where TSig : ISignal<TIn, TOut>
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