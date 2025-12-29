using System.Numerics;

namespace MeshWiz.Math.Signals;

public sealed record Gain<TIn, TOut>(ISignal<TIn,TOut> Source, TOut Value)
    : ISignal<TIn, TOut>
    where TIn : unmanaged, IFloatingPointIeee754<TIn>
    where TOut : unmanaged, IFloatingPointIeee754<TOut>
{
    public TOut Sample(TIn input) => Source.Sample(input) * Value;
}