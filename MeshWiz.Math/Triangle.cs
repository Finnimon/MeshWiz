using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Triangle<TVec, TNum> : ISurface<TVec, TNum>
    where TVec : unmanaged, IVec<TVec, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly TVec A, B, C;

    public Triangle(in TVec a, in TVec b, in TVec c)
    {
        A = a;
        B = b;
        C = c;
    }

    public ICurve<TVec, TNum> Bounds => new Polyline<TVec, TNum>([A, B, C,A]);


    public TVec Centroid => (A + B + C) / TNum.CreateTruncating(3);

    public TNum SurfaceArea
    {
        get
        {
            var ab = B - A;
            var ac = C - A;
            var abAcDot = ab.Dot(ac);
            return TNum.Sqrt((ab.Dot(ab)) * (ac.Dot(ac)) - abAcDot * abAcDot) / TNum.CreateTruncating(2);
        }
    }
}