using System.Numerics;

namespace MeshWiz.Math;

public interface IAnalyticSurface<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    Vec2<TNum> ProjectIntoLocal(Vec3<TNum> world);
    Vec3<TNum> ProjectIntoWorld(Vec2<TNum> local);
    Vec3<TNum> Clamp(Vec3<TNum> p);
    TNum CurvatureAt(Vec3<TNum> p);
    TNum CurvatureAt(Vec2<TNum> p);
    bool HasCurvature { get; }
}