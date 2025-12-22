using System.Numerics;

namespace MeshWiz.Signals;

public sealed record Shift<TSignal, TIn, TOut>(TSignal Signal, TOut Value)
    : ISignal<TIn, TOut>
    where TSignal : ISignal<TIn, TOut>
    where TIn : unmanaged, IFloatingPointIeee754<TIn>
    where TOut : unmanaged, IFloatingPointIeee754<TOut>
{
    public TOut Sample(TIn input) => Signal.Sample(input) + Value;
}