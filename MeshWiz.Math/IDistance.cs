using System.Diagnostics.Contracts;

namespace MeshWiz.Math;

public interface IDistance<TSelf, TNum>
    where TSelf : IDistance<TSelf, TNum>
{
    [Pure]
    TNum DistanceTo(TSelf other);

    [Pure]
    TNum SquaredDistanceTo(TSelf other);

    [Pure]
    static abstract TNum Distance(TSelf a, TSelf b);
    
    [Pure]
    static abstract TNum SquaredDistance(TSelf a, TSelf b);
}