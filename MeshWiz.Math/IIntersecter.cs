using System.Diagnostics.Contracts;

namespace MeshWiz.Math;

public interface IIntersectTest<in TIntersect>
{
    [Pure]
    bool DoIntersect(TIntersect test);
}
public interface IIntersecter<in TIntersect, TIntersection>: IIntersectTest<TIntersect>
{
    [Pure]
    public bool Intersect(TIntersect test, out TIntersection result);
}