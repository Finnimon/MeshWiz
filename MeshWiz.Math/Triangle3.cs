using System.Numerics;
using System.Runtime.InteropServices;
using MeshWiz.Contracts;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]

public readonly struct Triangle3<TNum>:ISurface<Vector3<TNum>, TNum>, IFlat<TNum>, IByteSize
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Vector3<TNum> A,B,C;
    public Vector3<TNum> Normal => ((B - A)^(C-A)).Normalized;
    public Plane3<TNum> Plane => new(in this);
    public Triangle3(Vector3<TNum> a,Vector3<TNum> b,Vector3<TNum> c)
    {
        A = a;
        B = b;
        C = c;
    }

    public ICurve<Vector3<TNum>, TNum> Bounds => new PolyLine<Vector3<TNum>, TNum>([A, B, C]);


    public Vector3<TNum> Centroid => (A + B + C) /TNum.CreateTruncating(3);
    public TNum SurfaceArea 
    {
        get
        {
            var ab = B - A;
            var ac = C - A;
            var abAcDot= ab*ac;
            return TNum.Sqrt((ab * ab) * (ac * ac)-abAcDot*abAcDot)/TNum.CreateTruncating(2);
        }
    }
    
    public static implicit operator Triangle3<TNum>(Triangle<Vector3<TNum>,TNum> dimensionless)
        =>new(dimensionless.A,dimensionless.B,dimensionless.C);

    public static int ByteSize =>Vector3<TNum>.ByteSize*3;

    public void Deconstruct(out Vector3<TNum> a, out Vector3<TNum> b, out Vector3<TNum> c)
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
    public BBox3<TNum> BBox=>BBox3<TNum>.FromPoint(A).CombineWith(B).CombineWith(C);
}