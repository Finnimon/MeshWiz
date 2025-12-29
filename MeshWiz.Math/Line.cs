using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using MeshWiz.Utility;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Line<TVec, TNum>(TVec Start, TVec End)
    : ILine<TVec, TNum>,IBounded<TVec>
    where TVec : unmanaged, IVec<TVec, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
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
    public TVec Direction => AxisVector.Normalized();

    [Pure]
    public Line<TVec, TNum> Reversed() => new(End, Start);

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    TNum IDiscreteCurve<TVec, TNum>.Length => AxisVector.Length;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TNum SquaredLength => AxisVector.SquaredLength;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public AABB<TVec> Bounds => AABB<TVec>.From(Start, End);

    [Pure]
    public static Line<TVec, TNum> FromAxisVector(TVec start, TVec direction)
        => new(start, start+direction);

    [Pure]
    public static Line<TVec, TNum> FromAxisVector(TVec direction)
        => new(TVec.Zero, direction);

    [Pure]
    public TVec Traverse(TNum t)
        => TVec.Lerp(Start,End,t);

    [Pure]
    public TVec TraverseOnCurve(TNum t)
        => Traverse(TNum.Clamp(t, TNum.Zero, TNum.One));

    [Pure]
    public static Line<TVec, TNum> operator +(Line<TVec, TNum> l, Line<TVec, TNum> r)
        => FromAxisVector(l.Start + r.Start, l.AxisVector + r.AxisVector);

    [Pure]
    public static Line<TVec, TNum> operator +(Line<TVec, TNum> l, TVec r)
        => new(l.Start + r, l.End + r);

    [Pure]
    public static Line<TVec, TNum> operator +(TVec l, Line<TVec, TNum> r)
        => r + l;

    [Pure]
    public static Line<TVec, TNum> operator -(Line<TVec, TNum> l, Line<TVec, TNum> r)
        => FromAxisVector(l.Start - r.Start, l.AxisVector - r.AxisVector);

    [Pure]
    public static Line<TVec, TNum> operator *(Line<TVec, TNum> l, TNum r)
        => FromAxisVector(l.Start * r, l.AxisVector * r);

    [Pure]
    public TNum DistanceTo(TVec p)
        => ClosestPoint(p).DistanceTo(p);
    
    [Pure]
    public TNum DistanceToSegment(TVec p)
        => ClosestPointOnSegment(p).DistanceTo(p);

    [Pure]
    public TNum SquaredDistanceTo(TVec p)
        => ClosestPoint(p).SquaredDistanceTo(p);

    [Pure]
    public TNum SquaredDistanceToSegment(TVec p)
        => ClosestPointOnSegment(p).SquaredDistanceTo(p);

    [Pure]
    public TVec ClosestPoint(TVec p)
    {
        var v = p - Start;
        var ndir = Direction;
        var dotProduct = v.Dot(ndir);
        var alongVector = dotProduct * ndir;
        return Start + alongVector;
    }

    [Pure]
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

    [Pure]
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

    [Pure]
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

    [Pure]
    public Line<TVec, TNum> Section(TNum start, TNum end)
        => new(Traverse(start),Traverse(end));

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

}

public static class Line
{
    [Pure]
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

    [Pure]
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

    [Pure]
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