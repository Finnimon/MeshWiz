using System.Diagnostics.Contracts;

namespace MeshWiz.Math;

public interface ILerp<TSelf, in TNum> 
    where TSelf : ILerp<TSelf, TNum>
{
    [Pure]
    static abstract TSelf Lerp(TSelf a, TSelf b, TNum t);

    [Pure]
    static abstract TSelf ExactLerp(TSelf a, TSelf b, TNum exactDistance);
}