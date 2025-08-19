using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Triangle<TVector, TNum> : ISurface<TVector, TNum>
    where TVector : unmanaged, IFloatingVector<TVector, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly TVector A, B, C;

    public Triangle(in TVector a, in TVector b, in TVector c)
    {
        A = a;
        B = b;
        C = c;
    }

    public ICurve<TVector, TNum> Bounds => new Polyline<TVector, TNum>([A, B, C]);


    public TVector Centroid => (A + B + C) / TNum.CreateTruncating(3);

    public TNum SurfaceArea
    {
        get
        {
            var ab = B - A;
            var ac = C - A;
            var abAcDot = ab * ac;
            return TNum.Sqrt((ab * ab) * (ac * ac) - abAcDot * abAcDot) / TNum.CreateTruncating(2);
        }
    }
}