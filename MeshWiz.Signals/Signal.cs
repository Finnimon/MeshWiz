using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using MeshWiz.Math;

namespace MeshWiz.Signals;

public static partial class Signal
{
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ClampedSignal<TIn, TOut> Clamped<TIn, TOut>(this ISignal<TIn,TOut> sig,
        AABB<TOut> clamp)
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => new(sig, clamp);
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CachingSignal<TIn, TOut> Cache<TIn, TOut>(this ISignal<TIn,TOut> sig)
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => new(sig);

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Gain<TIn, TOut> Gain<TIn, TOut>(this ISignal<TIn,TOut> sig,
        TOut gain)
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => new(sig, gain);
    
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InputGain<TIn, TOut> InputGain<TIn, TOut>(this ISignal<TIn,TOut> sig,
        TIn gain)
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => new(sig, gain);
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Shift<TIn, TOut> Shift<TIn, TOut>(this ISignal<TIn,TOut> sig,
        TOut shift)
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => new(sig, shift);
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SignalResult<TIn, TOut> GetResult<TIn, TOut>(this ISignal<TIn,TOut> sig,
        TIn input)
        where TIn : unmanaged, IFloatingPointIeee754<TIn>
        where TOut : unmanaged, IFloatingPointIeee754<TOut>
        => SignalResult<TIn, TOut>.Create(sig, input);

}
public sealed record InputGain<TIn,TOut>(ISignal<TIn,TOut> Underlying,TIn Scalar):ISignal<TIn,TOut> where TIn : unmanaged, IFloatingPointIeee754<TIn> where TOut : unmanaged, IFloatingPointIeee754<TOut>
{
    /// <inheritdoc />
    public TOut Sample(TIn input) => Underlying.Sample(input*Scalar);
}