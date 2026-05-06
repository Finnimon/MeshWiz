using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MeshWiz.Utility;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Triangle2<TNum>:ISurface<Vec2<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Vec2<TNum> A,B,C;
    public int Up  => (B - A).CrossSign(C-A);
    
    public Triangle2(Vec2<TNum> a,Vec2<TNum> b,Vec2<TNum> c)
    {
        A = a;
        B = b;
        C = c;
    }

    public ICurve<Vec2<TNum>, TNum> Bounds => new Polyline<Vec2<TNum>, TNum>([A, B, C]);


    public Vec2<TNum> Centroid => (A + B + C) * Numbers<TNum>.Third;
    public TNum SurfaceArea 
    {
        get
        {
            var ab = B - A;
            var ac = C - A;
            var abAcDot= ab.Dot(ac);
            return TNum.Sqrt((ab.Dot(ab)) * (ac.Dot(ac))-abAcDot*abAcDot) * Numbers<TNum>.Half;
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
    
    
    [System.Diagnostics.Contracts.Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Triangle2<TOther> To<TOther>()
        where TOther : unmanaged, IFloatingPointIeee754<TOther>
        => new(A.To<TOther>(),B.To<TOther>(),C.To<TOther>());

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
public static bool DoIntersect(Triangle2<TNum> t1, Triangle2<TNum> t2)
{
    static bool OnDifferentSides(Vec2<TNum> p1, Vec2<TNum> p2, Vec2<TNum> a, Vec2<TNum> b)
    {
        var ab = b - a;
        var cp1 = (p1 - a).Cross(ab);
        var cp2 = (p2 - a).Cross(ab);
        return cp1 * cp2 <= TNum.Zero;
    }

    static bool EdgeIntersect(Vec2<TNum> a1, Vec2<TNum> a2, Vec2<TNum> b1, Vec2<TNum> b2)
    {
        return OnDifferentSides(a1, a2, b1, b2) && OnDifferentSides(b1, b2, a1, a2);
    }

    var t1AsSpan = MemoryMarshal.CreateReadOnlySpan(in t1.A, 3);
    var t2AsSpan = MemoryMarshal.CreateReadOnlySpan(in t2.A, 3);

    for (var i = 0; i < 3; i++)
    {
        var aStart = t1AsSpan[i];
        var aEnd = t1AsSpan[(i + 1)%3];
        for (var j = 0; j < 3; j++)
        {
            var bStart = t2AsSpan[j];
            var bEnd = t2AsSpan[(j+1)%3];
            if (EdgeIntersect(aStart, aEnd, bStart, bEnd))
                return true;
        }
    }

    static bool PointInTriangle(Vec2<TNum> p, Triangle2<TNum> tri)
    {
        var v0 = tri.C - tri.A;
        var v1 = tri.B - tri.A;
        var v2 = p - tri.A;

        var dot00 = v0.Dot(v0);
        var dot01 = v0.Dot(v1);
        var dot02 = v0.Dot(v2);
        var dot11 = v1.Dot(v1);
        var dot12 = v1.Dot(v2);

        var denom = dot00 * dot11 - dot01 * dot01;
        if (denom == TNum.Zero) return false; // degenerate triangle

        var invDenom = TNum.One / denom;
        var u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        var v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        return u >= TNum.Zero && v >= TNum.Zero && (u + v) <= TNum.One;
    }

    if (PointInTriangle(t1.A, t2) || PointInTriangle(t2.A, t1))
        return true;

    return false;
}
}