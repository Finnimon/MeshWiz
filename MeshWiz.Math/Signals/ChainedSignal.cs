using System.Numerics;

namespace MeshWiz.Math.Signals;

public sealed record ChainedSignal<TIn, TIntermediate, TOut>(
    ISignal<TIn, TIntermediate> Left,
    ISignal<TIntermediate, TOut> Right
)
    : ISignal<TIn, TOut>
    where TIn : unmanaged, IFloatingPointIeee754<TIn>
    where TIntermediate : unmanaged, IFloatingPointIeee754<TIntermediate>
    where TOut : unmanaged, IFloatingPointIeee754<TOut>
{
    /// <inheritdoc />
    public TOut Sample(TIn input) => Right.Sample(Left.Sample(input));
}