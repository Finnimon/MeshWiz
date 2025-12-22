using System.Numerics;
using MeshWiz.Math;

namespace MeshWiz.Signals;

public sealed record ClampedSignal<TIn, TOut>(ISignal<TIn,TOut> Source, AABB<TOut> Bounds)
    : ISignal<TIn, TOut>
    where TIn : unmanaged, IFloatingPointIeee754<TIn>
    where TOut : unmanaged, IFloatingPointIeee754<TOut>
{
    public TOut Sample(TIn input) => Bounds.Clamp(Source.Sample(input));
}