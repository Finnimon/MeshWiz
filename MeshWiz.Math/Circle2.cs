using System.Numerics;
using MeshWiz.Utility;

namespace MeshWiz.Math;

public readonly struct Circle2<TNum> : MeshWiz.Math.ISurface<Vector2<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Vector2<TNum> Center;
    public readonly TNum Radius;
    public TNum Diameter => Radius * Numbers<TNum>.Two;
    public TNum Circumference => Radius * Numbers<TNum>.TwoPi;
    public TNum SurfaceArea => Radius * Radius * TNum.Pi;

    public Circle2(Vector2<TNum> center, TNum radius)
    {
        Center = center;
        Radius = radius;
    }

    public Vector2<TNum> Centroid => Center;
}