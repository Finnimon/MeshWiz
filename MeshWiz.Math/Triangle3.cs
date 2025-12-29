using System.Numerics;
using System.Runtime.InteropServices;
using MeshWiz.Contracts;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]

public readonly struct Triangle3<TNum>:ISurface<Vec3<TNum>, TNum>, IFlat<TNum>, IByteSize
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Vec3<TNum> A,B,C;
    public Vec3<TNum> Normal => ((B - A).Cross(C-A)).Normalized();
    public Plane3<TNum> Plane => new(in this);
    public Triangle3(Vec3<TNum> a,Vec3<TNum> b,Vec3<TNum> c)
    {
        A = a;
        B = b;
        C = c;
    }

    public ICurve<Vec3<TNum>, TNum> Bounds => new Polyline<Vec3<TNum>, TNum>([A, B, C]);


    public Vec3<TNum> Centroid => (A + B + C) /TNum.CreateTruncating(3);
    public TNum SurfaceArea 
    {
        get
        {
            var ab = B - A;
            var ac = C - A;
            var abAcDot= ab.Dot(ac);
            return TNum.Sqrt((ab.Dot(ab)) * (ac.Dot(ac))-abAcDot*abAcDot)/TNum.CreateTruncating(2);
        }
    }
    
    public static implicit operator Triangle3<TNum>(Triangle<Vec3<TNum>,TNum> dimensionless)
        =>new(dimensionless.A,dimensionless.B,dimensionless.C);

    public static int ByteSize =>Vec3<TNum>.ByteSize*3;

    public void Deconstruct(out Vec3<TNum> a, out Vec3<TNum> b, out Vec3<TNum> c)
    {
        a = A;
        b = B;
        c = C;
    }
    public (TNum dAB, TNum dBC, TNum dCA) EdgeLengths()
    {
        var ab = B.Subtract(A).Length;
        var bc = C.Subtract(B).Length;
        var ca = A.Subtract(B).Length;
        return (ab,bc,ca);
    }
    public AABB<Vec3<TNum>> BBox=>AABB<Vec3<TNum>>.From(A,B,C);

    public Triangle3<TOtherNum> To<TOtherNum>() 
        where TOtherNum : unmanaged, IFloatingPointIeee754<TOtherNum> =>
        new(A.To<TOtherNum>(), B.To<TOtherNum>(), C.To<TOtherNum>());
}