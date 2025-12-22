using System.Numerics;

namespace MeshWiz.Signals;

public readonly record struct Gain<TSignal, TIn, TOut>(TSignal Source, TOut Value)
    : ISignal<TIn, TOut>
    where TSignal : ISignal<TIn, TOut>
    where TIn : unmanaged, IFloatingPointIeee754<TIn>
    where TOut : unmanaged, IFloatingPointIeee754<TOut>
{
    public TOut Sample(TIn input) => Source.Sample(input) * Value;
}