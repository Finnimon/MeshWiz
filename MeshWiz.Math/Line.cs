using System;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using MeshWiz.Utility;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Line<TVec, TNum>(TVec start, TVec end)
    : ILine<TVec, TNum>, IBounded<TVec>, IEquatable<Line<TVec, TNum>>
    where TVec : unmanaged, IVec<TVec, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly TVec Start = start, End = end;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    TVec IDiscreteCurve<TVec, TNum>.Start => Start;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    TVec IDiscreteCurve<TVec, TNum>.End => End;

    public Line<TVec, TNum> Normalized() => FromAxisVector(Start, Direction);

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TVec MidPoint => (Start + End) * Numbers<TNum>.Half;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    bool ICurve<TVec, TNum>.IsClosed => false;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TNum Length => AxisVector.Length;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TVec AxisVector => End - Start;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TVec Direction => TVec.Normalize(End - Start);

    [Pure]
    public Line<TVec, TNum> Reversed() => new(End, Start);

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    TNum IDiscreteCurve<TVec, TNum>.Length => AxisVector.Length;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TNum SquaredLength => AxisVector.SquaredLength;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public AABB<TVec> Bounds => AABB<TVec>.From(Start, End);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Line<TVec, TNum> FromAxisVector(TVec start, TVec direction)
        => new(start, start + direction);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out TVec start, out TVec end)
    {
        start = Start;
        end = End;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Line<TVec, TNum> FromAxisVector(TVec direction)
        => new(TVec.Zero, direction);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TVec Traverse(TNum t)
        => TVec.Lerp(Start, End, t);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TVec TraverseOnCurve(TNum t)
        => Traverse(TNum.Clamp(t, TNum.Zero, TNum.One));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Line<TVec, TNum> operator +(Line<TVec, TNum> l, Line<TVec, TNum> r)
        => FromAxisVector(l.Start + r.Start, l.AxisVector + r.AxisVector);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Line<TVec, TNum> operator +(Line<TVec, TNum> l, TVec r)
        => new(l.Start + r, l.End + r);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Line<TVec, TNum> operator +(TVec l, Line<TVec, TNum> r)
        => r + l;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Line<TVec, TNum> operator -(Line<TVec, TNum> l, Line<TVec, TNum> r)
        => FromAxisVector(l.Start - r.Start, l.AxisVector - r.AxisVector);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Line<TVec, TNum> operator *(Line<TVec, TNum> l, TNum r)
        => FromAxisVector(l.Start * r, l.AxisVector * r);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum DistanceTo(TVec p)
        => ClosestPoint(p).DistanceTo(p);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum DistanceToSegment(TVec p)
        => ClosestPointOnSegment(p).DistanceTo(p);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum SquaredDistanceTo(TVec p)
        => ClosestPoint(p).SquaredDistanceTo(p);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum SquaredDistanceToSegment(TVec p)
        => ClosestPointOnSegment(p).SquaredDistanceTo(p);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TVec ClosestPoint(TVec p)
    {
        var v = p - Start;
        var ndir = Direction;
        var dotProduct = v.Dot(ndir);
        var alongVector = dotProduct * ndir;
        return Start + alongVector;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TVec ClosestPointOnSegment(TVec p)
    {
        var v = p - Start;
        var direction = AxisVector;
        var length = direction.Length;
        var ndir = direction / length;
        var dotProduct = v.Dot(ndir);
        dotProduct = TNum.Clamp(dotProduct, TNum.Zero, length);
        var alongVector = dotProduct * ndir;
        return Start + alongVector;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (TVec closest, TVec onSeg) ClosestPoints(TVec p)
    {
        var v = p - Start;
        var direction = AxisVector;
        var length = direction.Length;
        var ndir = direction / length;
        var dotProduct = v.Dot(ndir);
        var closest = ndir * dotProduct + Start;
        dotProduct = TNum.Clamp(dotProduct, TNum.Zero, length);
        var onSeg = Start + dotProduct * ndir;
        return (closest, onSeg);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (TNum closest, TNum onSeg) GetClosestPositions(TVec p)
    {
        var v = p - Start;
        var direction = AxisVector;
        var length = direction.Length;
        var ndir = direction / length;
        var closest = v.Dot(ndir) / length;
        var onSeg = TNum.Clamp(closest, TNum.Zero, TNum.One);
        return (closest, onSeg);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Line<TVec, TNum> Section(TNum start, TNum end)
        => new(Traverse(start), Traverse(end));

    public Polyline<TVec, TNum> ToPolyline() => new(Start, End);
    public Polyline<TVec, TNum> ToPolyline(PolylineTessellationParameter<TNum> _) => new(Start, End);

    /// <inheritdoc />
    public TVec GetTangent(TNum t)
        => Direction;

    /// <inheritdoc />
    public TVec EntryDirection => Direction;

    /// <inheritdoc />
    public TVec ExitDirection => Direction;

    /// <inheritdoc />
    public AABB<TVec> BBox => AABB.From(Start, End);

    public Line<TOtherVec, TOther> To<TOtherVec, TOther>()
        where TOtherVec : unmanaged, IVec<TOtherVec, TOther>
        where TOther : unmanaged, IFloatingPointIeee754<TOther> =>
        new(TOtherVec.FromComponentsConstrained<TVec, TNum>(Start),
            TOtherVec.FromComponentsConstrained<TVec, TNum>(End));

    /// <inheritdoc />
    public bool Equals(Line<TVec, TNum> other) => this == other;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Line<TVec, TNum> other && this == other;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Start, End);

    public static bool operator ==(Line<TVec, TNum> left, Line<TVec, TNum> right) =>
        left.Start == right.Start && left.End == right.End;

    public static bool operator !=(Line<TVec, TNum> left, Line<TVec, TNum> right) =>
        left.Start != right.Start || left.End != right.End;

    /// <inheritdoc />
    public override string ToString() => FormattableString.Invariant($"Line {{ Start = {Start}, End = {End} }}");
}

public static class Line
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryIntersect<TNum>(
        in Line<Vec2<TNum>, TNum> a,
        in Line<Vec2<TNum>, TNum> b,
        out TNum alongA)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        alongA = default;

        var p = a.Start;
        var r = a.AxisVector;
        var q = b.Start;
        var s = b.AxisVector;
        var pq = q - p;
        var rxs = r.Cross(s);

        var colinear = TNum.Abs(rxs) < TNum.CreateTruncating(1e-8);
        if (colinear) return false;

        alongA = pq.Cross(s) / rxs;

        return true;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryIntersectOnSegment<TNum>(
        in Line<Vec2<TNum>, TNum> a,
        in Line<Vec2<TNum>, TNum> b,
        out TNum alongA)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        alongA = default;

        var p = a.Start;
        var r = a.AxisVector;
        var q = b.Start;
        var s = b.AxisVector;
        var pq = q - p;
        var rxs = r.Cross(s);

        var colinear = TNum.Abs(rxs) < TNum.CreateTruncating(1e-8);
        if (colinear) return false;

        var t = pq.Cross(s) / rxs;

        rxs = -rxs;
        pq = -pq;
        var t2 = pq.Cross(r) / rxs;

        alongA = t;
        return TNum.Clamp(t, TNum.Zero, TNum.One) == t
               && TNum.Clamp(t2, TNum.Zero, TNum.One) == t2;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryIntersectOnSegment<TNum>(
        in Line<Vec2<TNum>, TNum> a,
        in Line<Vec2<TNum>, TNum> b,
        out TNum alongA,
        out TNum alongB)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        alongA = default;
        alongB = default;
        var p = a.Start;
        var r = a.AxisVector;
        var q = b.Start;
        var s = b.AxisVector;
        var pq = q - p;
        var rxs = r.Cross(s);

        var colinear = TNum.Abs(rxs) < TNum.CreateTruncating(1e-8);
        if (colinear)
            return false;

        var t = pq.Cross(s) / rxs;

        rxs = -rxs;
        pq = -pq;
        var t2 = pq.Cross(r) / rxs;

        alongA = t;
        alongB = t2;
        return TNum.Clamp(t, TNum.Zero, TNum.One) == t
               && TNum.Clamp(t2, TNum.Zero, TNum.One) == t2;
    }
}