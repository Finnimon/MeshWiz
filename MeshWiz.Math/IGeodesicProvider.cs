using System.Numerics;

namespace MeshWiz.Math;

public interface IGeodesicProvider<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    IPoseCurve<Pose3<TNum>, Vector3<TNum>, TNum> GetGeodesic(Vector3<TNum> p1, Vector3<TNum> p2);

    IPoseCurve<Pose3<TNum>, Vector3<TNum>, TNum>
        GetGeodesicFromEntry(Vector3<TNum> entryPoint, Vector3<TNum> direction);
}

public interface IGeodesicProvider<out TCurve, TNum> : IGeodesicProvider<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
    where TCurve : IPoseCurve<Pose3<TNum>, Vector3<TNum>, TNum>
{
    new TCurve GetGeodesic(Vector3<TNum> p1, Vector3<TNum> p2);
    new TCurve GetGeodesicFromEntry(Vector3<TNum> entryPoint, Vector3<TNum> direction);

    /// <inheritdoc />
    IPoseCurve<Pose3<TNum>, Vector3<TNum>, TNum> IGeodesicProvider<TNum>.GetGeodesic(Vector3<TNum> p1, Vector3<TNum> p2)
        => GetGeodesic(p1, p2);

    /// <inheritdoc />
    IPoseCurve<Pose3<TNum>, Vector3<TNum>, TNum> IGeodesicProvider<TNum>.GetGeodesicFromEntry(Vector3<TNum> entryPoint,
        Vector3<TNum> direction)
        => GetGeodesicFromEntry(entryPoint, direction);
}