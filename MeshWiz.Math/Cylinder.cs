using System.Numerics;

namespace MeshWiz.Math;

public readonly struct Cylinder<TNum> : IBody<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly TNum Radius;
    public readonly Line<Vector3<TNum>, TNum> Axis;
    public TNum Height => Axis.Length;
    public Circle3<TNum> Top => new(Axis.End, Axis.Direction, Radius);
    public Circle3<TNum> Base => new(Axis.Start, Axis.Direction, Radius);
    public TNum SurfaceArea => Top.Circumference * Axis.Length + Top.SurfaceArea * Numbers<TNum>.Two;
    public TNum Volume => Top.SurfaceArea * Axis.Length;
    public Vector3<TNum> Centroid => Axis.MidPoint;
    public AABB<Vector3<TNum>> BBox => Base.BBox.CombineWith(Top.BBox);

    public Cylinder(Line<Vector3<TNum>, TNum> axis, TNum radius)
    {
        if (TNum.IsPositive(radius))
        {
            Radius = radius;
            Axis = axis;
            return;
        }

        Radius = -radius;
        Axis = axis.Reversed();
    }

    public IMesh<TNum> Tessellate() => ConeSection<TNum>.TessellateCylindrical(Base, Top, 32);
}