namespace MeshWiz.Math;

public interface IIntersecter<in TIntersect, TIntersection>
{
    public bool DoIntersect(TIntersect test);
    public bool Intersect(TIntersect test, out TIntersection result);
}