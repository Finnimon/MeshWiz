using System.Numerics;
using System.Runtime.InteropServices;
using MeshWiz.Contracts;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]

public readonly struct Triangle3<TNum>:IFace<Vector3<TNum>, TNum>, IFlat<Vector3<TNum>>, IByteSize
    where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum>
{
    public readonly Vector3<TNum> A,B,C;
    public Vector3<TNum> Normal => ((B - A)^(C-A)).Normalized;
    
    public Triangle3(in Vector3<TNum> a,in Vector3<TNum> b,in Vector3<TNum> c)
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
}