using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using MeshWiz.Utility;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Line<TVector, TNum>(TVector Start, TVector End)
    : ILine<TVector, TNum>, IContiguousDiscreteCurve<TVector, TNum>
    where TVector : unmanaged, IFloatingVector<TVector, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public Line<TVector, TNum> Normalized() => FromDirection(Start, NormalDirection);

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TVector MidPoint => (Start + End) * Numbers<TNum>.Half;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    bool ICurve<TVector, TNum>.IsClosed => false;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TNum Length => Direction.Length;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TVector Direction => End - Start;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TVector NormalDirection => Direction.Normalized;

    [Pure]
    public Line<TVector, TNum> Reversed() => new(End, Start);

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    TNum IDiscreteCurve<TVector, TNum>.Length => Direction.Length;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TNum SquaredLength => Direction.SquaredLength;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public AABB<TVector> Bounds => AABB<TVector>.From(Start, End);

    [Pure]
    public static Line<TVector, TNum> FromDirection(TVector start, TVector direction)
        => new(start, start.Add(direction));

    [Pure]
    public static Line<TVector, TNum> FromDirection(TVector direction)
        => new(TVector.Zero, direction);

    [Pure]
    public TVector Traverse(TNum distance)
        => TVector.Lerp(Start,End,distance);

    [Pure]
    public TVector TraverseOnCurve(TNum distance)
        => Traverse(TNum.Clamp(distance, TNum.Zero, TNum.One));

    [Pure]
    public static Line<TVector, TNum> operator +(Line<TVector, TNum> l, Line<TVector, TNum> r)
        => FromDirection(l.Start + r.Start, l.Direction + r.Direction);

    [Pure]
    public static Line<TVector, TNum> operator +(Line<TVector, TNum> l, TVector r)
        => new(l.Start + r, l.End + r);

    [Pure]
    public static Line<TVector, TNum> operator +(TVector l, Line<TVector, TNum> r)
        => r + l;

    [Pure]
    public static Line<TVector, TNum> operator -(Line<TVector, TNum> l, Line<TVector, TNum> r)
        => FromDirection(l.Start - r.Start, l.Direction - r.Direction);

    [Pure]
    public static Line<TVector, TNum> operator *(Line<TVector, TNum> l, TNum r)
        => FromDirection(l.Start * r, l.Direction * r);

    [Pure]
    public TNum DistanceTo(TVector p)
        => ClosestPoint(p).DistanceTo(p);

    [Pure]
    public TNum DistanceToSegment(TVector p)
        => ClosestPointOnSegment(p).DistanceTo(p);

    [Pure]
    public TNum SquaredDistanceTo(TVector p)
        => ClosestPoint(p).SquaredDistanceTo(p);

    [Pure]
    public TNum SquaredDistanceToSegment(TVector p)
        => ClosestPointOnSegment(p).SquaredDistanceTo(p);

    [Pure]
    public TVector ClosestPoint(TVector p)
    {
        var v = p - Start;
        var ndir = NormalDirection;
        var dotProduct = v.Dot(ndir);
        var alongVector = dotProduct * ndir;
        return Start + alongVector;
    }

    [Pure]
    public TVector ClosestPointOnSegment(TVector p)
    {
        var v = p - Start;
        var direction = Direction;
        var length = direction.Length;
        var ndir = direction / length;
        var dotProduct = v.Dot(ndir);
        dotProduct = TNum.Clamp(dotProduct, TNum.Zero, length);
        var alongVector = dotProduct * ndir;
        return Start + alongVector;
    }

    [Pure]
    public (TVector closest, TVector onSeg) ClosestPoints(TVector p)
    {
        var v = p - Start;
        var direction = Direction;
        var length = direction.Length;
        var ndir = direction / length;
        var dotProduct = v.Dot(ndir);
        var closest = ndir * dotProduct + Start;
        dotProduct = TNum.Clamp(dotProduct, TNum.Zero, length);
        var onSeg = Start + dotProduct * ndir;
        return (closest, onSeg);
    }

    [Pure]
    public (TNum closest, TNum onSeg) GetClosestPositions(TVector p)
    {
        var v = p - Start;
        var direction = Direction;
        var length = direction.Length;
        var ndir = direction / length;
        var closest = v.Dot(ndir) / length;
        var onSeg = TNum.Clamp(closest, TNum.Zero, TNum.One);
        return (closest, onSeg);
    }

    [Pure]
    public Line<TVector, TNum> Section(TNum start, TNum end)
        => new(Traverse(start),Traverse(end));

    public Polyline<TVector, TNum> ToPolyline() => new(Start, End);
    public Polyline<TVector, TNum> ToPolyline(PolylineTessellationParameter<TNum> _) => new(Start, End);

    /// <inheritdoc />
    public TVector GetTangent(TNum _)
        => NormalDirection;

    /// <inheritdoc />
    public TVector EntryDirection => NormalDirection;

    /// <inheritdoc />
    public TVector ExitDirection => NormalDirection;
}

public static class Line
{
    [Pure]
    public static bool TryIntersect<TNum>(
        in Line<Vector2<TNum>, TNum> a,
        in Line<Vector2<TNum>, TNum> b,
        out TNum alongA)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        alongA = default;

        var p = a.Start;
        var r = a.Direction;
        var q = b.Start;
        var s = b.Direction;
        var pq = q - p;
        var rxs = r.Cross(s);

        var colinear = TNum.Abs(rxs) < TNum.CreateTruncating(1e-8);
        if (colinear) return false;

        alongA = pq.Cross(s) / rxs;

        return true;
    }

    [Pure]
    public static bool TryIntersectOnSegment<TNum>(
        in Line<Vector2<TNum>, TNum> a,
        in Line<Vector2<TNum>, TNum> b,
        out TNum alongA)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        alongA = default;

        var p = a.Start;
        var r = a.Direction;
        var q = b.Start;
        var s = b.Direction;
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
        in Line<Vector2<TNum>, TNum> a,
        in Line<Vector2<TNum>, TNum> b,
        out TNum alongA,
        out TNum alongB)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        alongA = default;
        alongB = default;
        var p = a.Start;
        var r = a.Direction;
        var q = b.Start;
        var s = b.Direction;
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