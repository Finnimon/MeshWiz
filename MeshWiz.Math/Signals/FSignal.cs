using System.Numerics;
using System.Runtime.CompilerServices;

namespace MeshWiz.Math.Signals;

public record FSignal<TIn, TOut>(Func<TIn, TOut> Func)
    : ISignal<TIn, TOut>
    where TIn : unmanaged, IFloatingPointIeee754<TIn>
    where TOut : unmanaged, IFloatingPointIeee754<TOut>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TOut Sample(TIn input) => Func(input);

    public Func<TIn, TOut> AsFunc()
        => Func;

    public static implicit operator FSignal<TIn, TOut>(Func<TIn, TOut> f) => new(f);
    public static implicit operator Func<TIn, TOut>(FSignal<TIn, TOut> f) => f.Func;

}