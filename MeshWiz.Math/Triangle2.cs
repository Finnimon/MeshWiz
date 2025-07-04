using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Triangle2<TNum>:IFace<Vector2<TNum>, TNum>
    where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum>
{
    public readonly Vector2<TNum> A,B,C;
    public int Up  => (B - A).CrossSign(C-A);
    
    public Triangle2(in Vector2<TNum> a,in Vector2<TNum> b,in Vector2<TNum> c)
    {
        A = a;
        B = b;
        C = c;
    }

    public ICurve<Vector2<TNum>, TNum> Bounds => new PolyLine<Vector2<TNum>, TNum>([A, B, C]);


    public Vector2<TNum> Centroid => (A + B + C) /TNum.CreateTruncating(2);
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
    
    public static implicit operator Triangle2<TNum>(Triangle<Vector2<TNum>,TNum> dimensionless)
        =>new(dimensionless.A,dimensionless.B,dimensionless.C);
}