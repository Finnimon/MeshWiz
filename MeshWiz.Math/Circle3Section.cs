using System.Numerics;

namespace MeshWiz.Math;

public readonly struct Circle3Section<TNum> : IFlat<TNum>, ISurface3<TNum>, IRotationalSurface<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public Vector3<TNum> Centroid { get; }
    public Vector3<TNum> Normal { get; }
    public readonly TNum MinorRadius;
    public readonly TNum MajorRadius;
    public Circle3<TNum> Major => new(Centroid, Normal, MajorRadius);
    public Circle3<TNum> Minor => new(Centroid, Normal, MinorRadius);

    /// <param name="centroid">centroid</param>
    /// <param name="normal">circle normal</param>
    /// <param name="minorRadius">inner radius<paramref name="normal"/></param>
    /// <param name="majorRadius">outer radius</param>
    public Circle3Section(Vector3<TNum> centroid, Vector3<TNum> normal, TNum minorRadius, TNum majorRadius)
    {
        MinorRadius = TNum.Abs(minorRadius);
        MajorRadius = TNum.Abs(majorRadius);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(MajorRadius,MinorRadius,nameof(minorRadius));
        Centroid = centroid;
        Normal = TNum.Sign(minorRadius) == TNum.Sign(majorRadius) ? normal.Normalized : -normal.Normalized;
    }

    public Plane3<TNum> Plane => new(Normal, Centroid);
    public TNum SurfaceArea => ( MajorRadius * MajorRadius-MinorRadius * MinorRadius) * TNum.Pi;


    /// <inheritdoc />
    public AABB<Vector3<TNum>> BBox => Major.BBox;


    public IMesh<TNum> Tessellate() => Tessellate(32);

    public IIndexedMesh<TNum> Tessellate(int edgeCount)
        =>Surface.Rotational.Tessellate<Circle3Section<TNum>,TNum>(this,edgeCount);
    

    /// <inheritdoc />
    public IDiscreteCurve<Vector3<TNum>, TNum> SweepCurve => Major.TraverseByAngle(TNum.Zero).LineTo(Minor.TraverseByAngle(TNum.Zero));

    /// <inheritdoc />
    public Ray3<TNum> SweepAxis => new(Centroid, Normal);
}