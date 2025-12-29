using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Ray2<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Vec2<TNum> Origin, Direction;

    public Ray2(Vec2<TNum> origin, Vec2<TNum> direction)
    {
        Origin = origin;
        Direction = direction.Normalized();
    }

    [Pure]
    public Vec2<TNum> Traverse(TNum distance)
        => Vec2<TNum>.FusedMultiplyAdd(Direction, new Vec2<TNum>(distance),Origin);

    public bool HitTest(in Ray2<TNum> ray, out TNum t)
    {
        t = TNum.NaN;

        var r = Direction;
        var s = ray.Direction;
        var delta = ray.Origin - Origin;

        var rxs = Vec2<TNum>.Cross(r, s);
        if (TNum.Abs(rxs) < TNum.Epsilon)
            return false; // Parallel or colinear

        var tNumerator = Vec2<TNum>.Cross(delta , s);
        t = tNumerator / rxs;

        return t >= TNum.Zero;
    }


    public static Ray2<TNum> operator -(Ray2<TNum> ray) => new(ray.Origin, -ray.Direction);

    public static implicit operator Ray2<TNum>(in Line<Vec2<TNum>, TNum> line)
        => new(line.Start, line.AxisVector);

    public static implicit operator Line<Vec2<TNum>, TNum>(in Ray2<TNum> ray) =>
        new(ray.Origin, ray.Origin + ray.Direction);

    [Pure]
    public bool TryIntersect(in Arc2<TNum> arc, out TNum t)
    {
        t = default;
        var circleDoesIntersect = TryIntersect(arc.Underlying, out var t1, out var t2);
        if (arc.IsAtLeastFullCircle) return circleDoesIntersect;
        var angleBoundary = arc.AngleBoundary;
        var t1Valid = TestPointOnArc(in arc, in angleBoundary, t1);
        var t2Valid = TestPointOnArc(in arc, in angleBoundary, t2);
        (var hit, t) = (t1Valid, t2Valid) switch
        {
            (false, false) => (false, t),
            (true, false) => (true, t1),
            (false, true) => (true, t2),
            (true, true) => (true, TNum.Min(t1, t2))
        };
        t = TNum.Max(TNum.Zero, t); //clamp to zero
        return hit;
    }

    private bool TestPointOnArc(in Arc2<TNum> arc, in AABB<TNum> angleRange, TNum t)
    {
        if (!t.IsApproxGreaterOrEqual(TNum.Zero)) return false;
        var p = Traverse(t);
        var polarAroundArc = (p - arc.CircumCenter).CartesianToPolar();
        Angle<TNum> angle = polarAroundArc.PolarAngle;
        return angle.IsIn(angleRange);
    }

    [Pure]
    public bool TryIntersect(in Circle2<TNum> circle, out TNum t)
    {
        t = default;
        var hit = TryIntersect(circle, out var t1, out var t2);
        if (!hit) return false;
        if (!t1.IsApproxGreaterOrEqual(TNum.Zero)) t = t2;
        else if (!t2.IsApproxGreaterOrEqual(TNum.Zero)) t = t1;
        else t = TNum.Min(t1, t2);
        return true;
    }

    public bool TryIntersect(in Circle2<TNum> circle, out TNum t, out TNum t2)
    {
        var delta = Origin - circle.Center;
        var deltaLen = delta.Length;
        var dirDotDelta = Direction.Dot(delta);
        var dirLen = TNum.One; //normal for ray
        var dirLenSqr = dirLen * dirLen;
        var dirDotDeltaSqr = dirDotDelta * dirDotDelta;
        var deltaLenSqr = deltaLen * deltaLen;
        var radiusSqr = circle.Radius * circle.Radius;
        var sqrtComponent = dirDotDeltaSqr - dirLenSqr * (deltaLenSqr - radiusSqr);
        t = TNum.NaN;
        t2 = TNum.NaN;
        if (!sqrtComponent.IsApproxGreaterOrEqual(TNum.Zero))
            return false;
        var negDirDotDelta = delta.Dot(-Direction);
        sqrtComponent = TNum.Max(sqrtComponent, TNum.Zero);
        sqrtComponent = TNum.Sqrt(sqrtComponent);
        t = (negDirDotDelta + sqrtComponent) / dirLenSqr;
        t2 = (negDirDotDelta - sqrtComponent) / dirLenSqr;
        (t, t2) = AABB.From(t, t2);
        return t.IsApproxGreaterOrEqual(TNum.Zero) || t2.IsApproxGreaterOrEqual(TNum.Zero);
    }

    public bool TryIntersect(in Line<Vec2<TNum>, TNum> otherLine, out TNum t)
    {
        var hit = Line.TryIntersect(this, in otherLine, out t);
        if (!hit || !t.IsApproxGreaterOrEqual(TNum.Zero)) return false;
        t = TNum.Max(t, TNum.Zero);
        return true;
    }
    
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec2<TNum> ClosestPoint(Vec2<TNum> p)
    {
        var v = p - Origin;
        var ndir = Direction;
        var dotProduct = v.Dot(ndir);
        var alongVector = dotProduct * ndir;
        return Origin + alongVector;
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum DistanceTo(Vec2<TNum> p) => ClosestPoint(p).DistanceTo(p);

}