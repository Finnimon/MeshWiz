using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Ray2<TNum> 
    where TNum : unmanaged, IFloatingPointIeee754<TNum> 
{
    public readonly Vector2<TNum> Origin, Direction;
    
    public Ray2( Vector2<TNum> origin, Vector2<TNum> direction)
    {
        Origin = origin;
        Direction = direction.Normalized;
    }
    
    
    public Vector2<TNum> Traverse(TNum distance)
        =>Origin+Direction*distance;

    public bool HitTest(in Ray2<TNum> ray, out TNum t)
    {
        t = TNum.NaN;

        var r = Direction;
        var s = ray.Direction;
        var delta = ray.Origin - Origin;

        var rxs = r ^ s;
        if (TNum.Abs(rxs) < TNum.Epsilon)
            return false; // Parallel or colinear

        var tNumerator = delta^s;
        t = tNumerator / rxs;

        return t >= TNum.Zero;
    }

    
    public static Ray2<TNum> operator -(Ray2<TNum> ray)=>new(ray.Origin, -ray.Direction);
    
    public static implicit operator Ray2<TNum>(in Line<Vector2<TNum>,TNum> line)
        =>new(line.Start, line.Direction);
}