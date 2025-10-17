using System.Numerics;

namespace MeshWiz.Math;

public interface IGeodesicProvider<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public IContiguousCurve<Vector3<TNum>, TNum> GetGeodesic(Vector3<TNum> p1, Vector3<TNum> p2);
    public IContiguousCurve<Vector3<TNum>, TNum> GetGeodesicFromEntry(Vector3<TNum> entryPoint, Vector3<TNum> direction);
}

public interface IGeodesicProvider<TCurve, TNum> :IGeodesicProvider<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
    where TCurve:IContiguousCurve<Vector3<TNum>, TNum>
{
    public new TCurve GetGeodesic(Vector3<TNum> p1, Vector3<TNum> p2);
    public new TCurve GetGeodesicFromEntry(Vector3<TNum> entryPoint, Vector3<TNum> direction);

    /// <inheritdoc />
    IContiguousCurve<Vector3<TNum>, TNum> IGeodesicProvider<TNum>.GetGeodesic(Vector3<TNum> p1, Vector3<TNum> p2) 
        => GetGeodesic(p1, p2);

    /// <inheritdoc />
    IContiguousCurve<Vector3<TNum>, TNum> IGeodesicProvider<TNum>.GetGeodesicFromEntry(Vector3<TNum> entryPoint, Vector3<TNum> direction) 
        => GetGeodesicFromEntry(entryPoint, direction);
}