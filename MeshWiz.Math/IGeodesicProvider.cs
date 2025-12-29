using System.Numerics;

namespace MeshWiz.Math;

public interface IGeodesicProvider<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    IPoseCurve<Pose3<TNum>, Vec3<TNum>, TNum> GetGeodesic(Vec3<TNum> p1, Vec3<TNum> p2);

    IPoseCurve<Pose3<TNum>, Vec3<TNum>, TNum>
        GetGeodesicFromEntry(Vec3<TNum> entryPoint, Vec3<TNum> direction);
}

public interface IGeodesicProvider<out TCurve, TNum> : IGeodesicProvider<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
    where TCurve : IPoseCurve<Pose3<TNum>, Vec3<TNum>, TNum>
{
    new TCurve GetGeodesic(Vec3<TNum> p1, Vec3<TNum> p2);
    new TCurve GetGeodesicFromEntry(Vec3<TNum> entryPoint, Vec3<TNum> direction);

    /// <inheritdoc />
    IPoseCurve<Pose3<TNum>, Vec3<TNum>, TNum> IGeodesicProvider<TNum>.GetGeodesic(Vec3<TNum> p1, Vec3<TNum> p2)
        => GetGeodesic(p1, p2);

    /// <inheritdoc />
    IPoseCurve<Pose3<TNum>, Vec3<TNum>, TNum> IGeodesicProvider<TNum>.GetGeodesicFromEntry(Vec3<TNum> entryPoint,
        Vec3<TNum> direction)
        => GetGeodesicFromEntry(entryPoint, direction);
}