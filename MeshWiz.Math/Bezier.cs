using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using CommunityToolkit.Diagnostics;
using MeshWiz.Buffers;
using MeshWiz.Collections;
using MeshWiz.RefLinq;

namespace MeshWiz.Math;

public static class Bezier
{
    public static T Lerp<T, TNum>(ReadOnlySpan<T> knots, TNum t)
        where T : ILerp<T, TNum>
    {
        return knots.Length switch
        {
            < 2 => ThrowHelper.ThrowArgumentException<T>(nameof(knots)),
            2 => T.Lerp(knots[0], knots[1], t),
            3 => Lerp(knots[0], knots[1], knots[2], t),
            4 => Lerp(knots[0], knots[1], knots[2], knots[3], t),
            _ => DeCasteljau(knots, t)
        };
    }
    
    
    public static T Lerp<T, TNum>(T a, T b, T c, T d, TNum t) where T : ILerp<T, TNum>
    {
        var midControl = T.Lerp(b, c, t);
        var left = T.Lerp(T.Lerp(a, b, t), midControl, t);
        var right = T.Lerp(midControl, T.Lerp(c, d, t), t);
        return T.Lerp(left, right, t);
    }

    public static T Lerp<T, TNum>(T a, T b, T c, TNum t) where T : ILerp<T, TNum> =>
        T.Lerp(T.Lerp(a, b, t), T.Lerp(b, c, t), t);


    public static T Lerp<T>(T a, T b, T c, T d, T t) where T : IFloatingPointIeee754<T>
    {
        var midControl = T.Lerp(b, c, t);
        var left = T.Lerp(T.Lerp(a, b, t), midControl, t);
        var right = T.Lerp(midControl, T.Lerp(c, d, t), t);
        return T.Lerp(left, right, t);
    }

    public static T Lerp<T>(T a, T b, T c, T t) where T : IFloatingPointIeee754<T> =>
        T.Lerp(T.Lerp(a, b, t), T.Lerp(b, c, t), t);

    private static T DeCasteljau<T, TNum>(ReadOnlySpan<T> knots, TNum t) where T : ILerp<T, TNum>
    {
        var tail = knots.Length;
        Debug.Assert(tail>0);
        using var buf = Pool.Rent<T>(tail*2);
        var lowerSpan = buf.Span.Slice(0,tail);
        var upperSpan = buf.Span.Slice(tail);
        ReadOnlySpan<T> lowerR = lowerSpan;
        ReadOnlySpan<T> upperR = upperSpan;
        ReadOnlySpan<T> compSpan;
        
        Span<T> target;
        if ((tail & 1) == 1)
        {
            knots.CopyTo(lowerSpan);
            compSpan = lowerSpan;
            target = upperSpan;
        }
        else
        {
            knots.CopyTo(upperSpan);
            compSpan = upperSpan;
            target = lowerSpan;
        }

        while (--tail != 0)
        {
            if ((tail & 1) == 0)
            {
                target = upperSpan;
                compSpan = lowerR;
            }
            else
            {
                target = lowerSpan;
                compSpan = upperR;
            }
            for (var i = 0; i < tail; i++)
                target[i] = T.Lerp(compSpan[i], compSpan[i + 1], t);
        }
        return target[0];
    }
    
    
    public static T Lerp<T>(ReadOnlySpan<T> knots, T t)
        where T : IFloatingPointIeee754<T>
    {
        return knots.Length switch
        {
            < 2 => ThrowHelper.ThrowArgumentException<T>(nameof(knots)),
            2 => T.Lerp(knots[0], knots[1], t),
            3 => Lerp(knots[0], knots[1], knots[2], t),
            4 => Lerp(knots[0], knots[1], knots[2], knots[3], t),
            _ => DeCasteljau(knots, t)
        };
    }

    
    private static T DeCasteljau<T>(ReadOnlySpan<T> knots, T t) where T : IFloatingPointIeee754<T>
    {
        var tail = knots.Length;
        Debug.Assert(tail>0);
        using var buf = Pool.Rent<T>(tail);
        var compSpan = buf.Span;
        knots.CopyTo(compSpan);
        while (--tail != 0)
            for (var i = 0; i < tail; i++)
                compSpan[i] = T.Lerp(compSpan[i], compSpan[i + 1], t);
        return compSpan[0];
    }
}

public sealed class BSpline<TVec,TNum>
where TVec:unmanaged,IVec<TVec,TNum>
where TNum: unmanaged, IFloatingPointIeee754<TNum>
{
    private readonly TVec[] _knots;
    public ReadOnlySpan<TVec> Knots => _knots;
    public TVec Traverse(TNum t) => Bezier.Lerp<TVec, TNum>(_knots, t);

    public BSpline(IEnumerable<TVec> knots) => _knots = knots.Iterate().ToArray();
}