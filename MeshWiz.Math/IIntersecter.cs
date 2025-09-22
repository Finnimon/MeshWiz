using System.Diagnostics.Contracts;

namespace MeshWiz.Math;

public interface IIntersecter<in TIntersect, TIntersection>
{
    [Pure]
    public bool DoIntersect(TIntersect test);
    [Pure]
    public bool Intersect(TIntersect test, out TIntersection result);
}