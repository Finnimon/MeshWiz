using System.Numerics;

namespace MeshWiz.Signals;

public interface ISignal<in TIn, out TOut>
    where TIn : unmanaged, IFloatingPointIeee754<TIn>
    where TOut : unmanaged, IFloatingPointIeee754<TOut>
{
    TOut Sample(TIn input);
    Func<TIn, TOut> AsFunc() => Sample;
}