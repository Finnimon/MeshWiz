using System.Numerics;

namespace MeshWiz.Math.Signals;

public sealed record InputGain<TIn, TOut>(ISignal<TIn, TOut> Underlying, TIn Scalar)
    : ISignal<TIn, TOut> where TIn : unmanaged, IFloatingPointIeee754<TIn>
    where TOut : unmanaged, IFloatingPointIeee754<TOut>
{
    /// <inheritdoc />
    public TOut Sample(TIn input) => Underlying.Sample(input * Scalar);
}