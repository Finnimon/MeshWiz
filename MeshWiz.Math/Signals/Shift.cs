using System.Numerics;

namespace MeshWiz.Math.Signals;

public sealed record Shift<TIn, TOut>(ISignal<TIn,TOut> Signal, TOut Value)
    : ISignal<TIn, TOut>
    where TIn : unmanaged, IFloatingPointIeee754<TIn>
    where TOut : unmanaged, IFloatingPointIeee754<TOut>
{
    public TOut Sample(TIn input) => Signal.Sample(input) + Value;
}