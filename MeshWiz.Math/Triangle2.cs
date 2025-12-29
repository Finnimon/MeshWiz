using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Triangle2<TNum>:ISurface<Vec2<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Vec2<TNum> A,B,C;
    public int Up  => (B - A).CrossSign(C-A);
    
    public Triangle2(in Vec2<TNum> a,in Vec2<TNum> b,in Vec2<TNum> c)
    {
        A = a;
        B = b;
        C = c;
    }

    public ICurve<Vec2<TNum>, TNum> Bounds => new Polyline<Vec2<TNum>, TNum>([A, B, C]);


    public Vec2<TNum> Centroid => (A + B + C) /TNum.CreateTruncating(2);
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
    
    public static implicit operator Triangle2<TNum>(Triangle<Vec2<TNum>,TNum> dimensionless)
        =>new(dimensionless.A,dimensionless.B,dimensionless.C);

    public (TNum dAB, TNum dBC, TNum dCA) EdgeLengths()
    {
        var ab = B.Subtract(A).Length;
        var bc = C.Subtract(B).Length;
        var ca = A.Subtract(B).Length;
        return (ab,bc,ca);
    }

    public void Deconstruct(out Vec2<TNum> a, out Vec2<TNum> b, out Vec2<TNum> c)
    {
        a = A;
        b = B;
        c = C;
    }
}