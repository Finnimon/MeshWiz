using System.Numerics;
using MeshWiz.Math;

namespace MeshWiz.Signals;

public readonly record struct ClampedSignal<TSignal, TIn, TOut>(TSignal Source, AABB<TOut> Bounds)
    : ISignal<TIn, TOut>
    where TSignal : ISignal<TIn, TOut>
    where TIn : unmanaged, IFloatingPointIeee754<TIn>
    where TOut : unmanaged, IFloatingPointIeee754<TOut>
{
    public TOut Sample(TIn input) => Bounds.Clamp(Source.Sample(input));
}