using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using MeshWiz.Math;

namespace MeshWiz.Signals;

public static partial class Signal
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ClampedSignal<TIn, TOut> Clamped<TIn, TOut>(this ISignal<TIn, TOut> sig,
        AABB<TOut> clamp)
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => new(sig, clamp);
    
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ClampedSignal<TIn, TOut> Saturate<TIn, TOut>(this ISignal<TIn, TOut> sig)
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => new(sig, AABB<TOut>.Saturate);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ISignal<TIn, TOut> Cache<TIn, TOut>(this ISignal<TIn, TOut> sig)
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => sig is IAsyncSignal<TIn, TOut> async
            ? new ConcurrentCachingSignal<TIn, TOut>(async)
            : new CachingSignal<TIn, TOut>(sig);

    
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IAsyncSignal<TIn, TOut> Cache<TIn, TOut>(this IAsyncSignal<TIn, TOut> sig)
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => new ConcurrentCachingSignal<TIn, TOut>(sig);



    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Gain<TIn, TOut> Gain<TIn, TOut>(this ISignal<TIn, TOut> sig,
        TOut gain)
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => new(sig, gain);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InputGain<TIn, TOut> InputGain<TIn, TOut>(this ISignal<TIn, TOut> sig,
        TIn gain)
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => new(sig, gain);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Shift<TIn, TOut> Shift<TIn, TOut>(this ISignal<TIn, TOut> sig,
        TOut shift)
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => new(sig, shift);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SignalResult<TIn, TOut> GetResult<TIn, TOut>(this ISignal<TIn, TOut> sig,
        TIn input)
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => SignalResult<TIn, TOut>.Create(sig, input);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ISignal<TIn, TOut> ChainWith<TIn, TIntermediate, TOut>(
        this ISignal<TIn, TIntermediate> left,
        ISignal<TIntermediate, TOut> right)
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TIntermediate : unmanaged, IFloatingPointIeee754<TIntermediate>
        where TOut : unmanaged, IFloatingPointIeee754<TOut> =>
        new ChainedSignal<TIn, TIntermediate, TOut>(left, right);

}