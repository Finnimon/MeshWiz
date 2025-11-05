using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using MeshWiz.Utility;

namespace MeshWiz.Math;

/// <summary>
/// Axis aligned Bounding Box
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct AABB<TNum>
    : IEqualityOperators<AABB<TNum>, AABB<TNum>, bool>,
        IEquatable<AABB<TNum>>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember]
    public static readonly AABB<TNum> Empty = new(TNum.PositiveInfinity, TNum.NegativeInfinity);

    public readonly TNum Min, Max;

    private AABB(TNum min, TNum max)
    {
        Min = min;
        Max = max;
    }

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TNum Size => Max - Min;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public bool IsEmpty => Size <= TNum.Zero;
    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public bool IsNegativeSpace => Size < TNum.NegativeZero;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TNum Center => Min + Size / Numbers<TNum>.Two;

    private AABB(TNum p) : this(p, p) { }

    [Pure]
    public AABB<TNum> CombineWith(TNum p)
    {
        var min = TNum.Min(p, Min);
        var max = TNum.Max(p, Max);
        return new AABB<TNum>(min, max);
    }

    [Pure]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public AABB<TNum> CombineWith(TNum p1, TNum p2)
    {
        var min = TNum.Min(p1, Min);
        min = TNum.Min(p2, min);
        var max = TNum.Max(p1, Max);
        max = TNum.Max(p2, max);
        return new AABB<TNum>(min, max);
    }

    [Pure]
    public AABB<TNum> CombineWith(TNum p1, TNum p2, TNum p3)
    {
        var min = TNum.Min(p1, Min);
        min = TNum.Min(p2, min);
        min = TNum.Min(p3, min);
        var max = TNum.Max(p1, Max);
        max = TNum.Max(p2, max);
        max = TNum.Max(p3, max);
        return new AABB<TNum>(min, max);
    }

    [Pure]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public AABB<TNum> CombineWith(TNum p1, TNum p2, TNum p3, TNum p4)
    {
        var min = TNum.Min(p1, Min);
        min = TNum.Min(p2, min);
        min = TNum.Min(p3, min);
        min = TNum.Min(p4, min);
        var max = TNum.Max(p1, Max);
        max = TNum.Max(p2, max);
        max = TNum.Max(p3, max);
        max = TNum.Max(p4, max);
        return new AABB<TNum>(min, max);
    }

    [Pure]
    public AABB<TNum> CombineWith(AABB<TNum> other)
        => new(TNum.Min(Min, other.Min), TNum.Max(Max, other.Max));

    [Pure]
    public AABB<TNum> CombineWith(AABB<TNum> aabb1, AABB<TNum> aabb2)
        => new(TNum.Min(TNum.Min(Min, aabb1.Min), aabb2.Min),
            TNum.Max(TNum.Max(Max, aabb1.Max), aabb2.Max));


    [Pure]
    public static AABB<TNum> Combine(AABB<TNum> a, AABB<TNum> b)
        => a.CombineWith(b);

    [Pure]
    public static AABB<TNum> Combine(AABB<TNum> a, AABB<TNum> b, AABB<TNum> c)
        => a.CombineWith(b, c);

    [Pure]
    public static AABB<TNum> Combine(AABB<TNum> a, TNum p)
        => a.CombineWith(p);

    [Pure]
    public static AABB<TNum> Combine(AABB<TNum> a, TNum p1, TNum p2)
        => a.CombineWith(p1, p2);

    [Pure]
    public static AABB<TNum> Combine(AABB<TNum> a, TNum p1, TNum p2, TNum p3)
        => a.CombineWith(p1, p2, p3);

    [Pure]
    public static AABB<TNum> Combine(AABB<TNum> a, TNum p1, TNum p2, TNum p3, TNum p4)
        => a.CombineWith(p1, p2, p3, p4);


    [Pure]
    public static AABB<TNum> From(TNum p)
        => new(p);

    [Pure]
    public static AABB<TNum> From(TNum p1, TNum p2)
    {
        var min = TNum.Min(p1, p2);
        var max = TNum.Max(p1, p2);
        return new(min, max);
    }

    [Pure]
    public static AABB<TNum> Around(TNum center, TNum size)
    {
        size = TNum.Abs(size) * Numbers<TNum>.Half;
        return From(center - size, center + size);
    }


    [Pure]
    public static AABB<TNum> From(TNum p1, TNum p2, TNum p3)
    {
        var min = TNum.Min(p1, p2);
        var max = TNum.Max(p1, p2);
        min = TNum.Min(min, p3);
        max = TNum.Max(max, p3);
        return new AABB<TNum>(min, max);
    }

    [Pure]
    public static AABB<TNum> From(TNum p1, TNum p2, TNum p3, TNum p4)
    {
        var min = TNum.Min(p1, p2);
        var max = TNum.Max(p1, p2);
        min = TNum.Min(min, p3);
        max = TNum.Max(max, p3);
        min = TNum.Min(min, p4);
        max = TNum.Max(max, p4);
        return new AABB<TNum>(min, max);
    }


    [Pure]
    public static AABB<TNum> From(params TNum[] pts)
        => pts.Length switch
        {
            0 => Empty,
            1 => From(pts[0]),
            2 => From(pts[0], pts[1]),
            3 => From(pts[0], pts[1], pts[2]),
            4 => From(pts[0], pts[1], pts[2], pts[3]),
            _ => FromNotEmptyArray(pts)
        };

    private static AABB<TNum> FromNotEmptyArray(TNum[] pts)
    {
        var min = pts[0];
        var max = pts[0];

        for (var i = 1; i < pts.Length; i++)
        {
            min = TNum.Min(min, min);
            max = TNum.Max(max, max);
        }

        return new AABB<TNum>(min, max);
    }

    [Pure]
    public bool Contains(TNum p) => Clamp(p) == p;

    [Pure]
    public bool Contains(TNum p, TNum epsilon) => TNum.Abs(Clamp(p) - p) < epsilon;

    [Pure]
    public bool IntersectsWith(AABB<TNum> other) => Contains(TNum.Clamp(Center, other.Min, other.Max));


    /// <inheritdoc />
    [Pure]
    public static bool operator ==(AABB<TNum> left, AABB<TNum> right)
        => left.Equals(right);

    /// <inheritdoc />
    [Pure]
    public static bool operator !=(AABB<TNum> left, AABB<TNum> right)
        => !left.Equals(right);

    /// <inheritdoc />
    [Pure]
    public bool Equals(AABB<TNum> other)
        => Min == other.Min && Max == other.Max;


    /// <inheritdoc />
    [Pure]
    public override bool Equals(object? obj) => obj is AABB<TNum> other && Equals(other);

    /// <inheritdoc />
    [Pure]
    public override int GetHashCode() => HashCode.Combine(Min, Max);

    [Pure]
    public TNum DistanceTo(TNum p)
        => TNum.Abs(p - Clamp(p));


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    public TNum Clamp(TNum value) => TNum.Max(Min,TNum.Min(value, Max));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    public TNum ClampToBounds(TNum value)
    {
        var minDiff=TNum.Abs(value - Min);
        var maxDiff=TNum.Abs(value - Max);
        return minDiff < maxDiff ? minDiff : maxDiff;
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    public AABB<TNum> Clamp(AABB<TNum> value) => new(Clamp(value.Min),Clamp(value.Max));

    public AABB<TOther> To<TOther>() where TOther : unmanaged, IFloatingPointIeee754<TOther>
    {
        return new AABB<TOther>(TOther.CreateTruncating(Min), TOther.CreateTruncating(Max));
    }

    public static AABB<TNum> Combine(IEnumerable<AABB<TNum>> select)
    {
        var min = TNum.PositiveInfinity;
        var max = TNum.NegativeInfinity;
        foreach (var aabb in select)
        {
            min = TNum.Min(min, aabb.Min);
            max = TNum.Max(max, aabb.Max);
        }

        return new AABB<TNum>(min, max);
    }
    
    public static AABB<TNum> operator +(AABB<TNum> l, TNum r)=>new(l.Min+r,l.Max+r); 
    public static AABB<TNum> operator +(TNum l, AABB<TNum> r)=>r+l;
    public static AABB<TNum> operator |(AABB<TNum> l, AABB<TNum> r) => l.CombineWith(r);

    public static AABB<TNum> operator &(AABB<TNum> l, AABB<TNum> r)
    {
        if (r.Max < l.Max) (l, r) = (r, l);
        var rcl= r.Clamp(l.Max);
        if (!l.Contains(rcl)) return Empty;
        return new AABB<TNum>(l.Clamp(r.Min), rcl);
    }

    public void Deconstruct(out TNum min, out TNum max)
    {
        min = Min;
        max = Max;
    }

    public static readonly AABB<TNum> Saturate = new(TNum.Zero, TNum.One);
}

public static class AABB
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AABB<TNum> From<TNum>(TNum p1)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        => AABB<TNum>.From(p1);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AABB<TNum> From<TNum>(TNum p1, TNum p2)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        => AABB<TNum>.From(p1, p2);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AABB<TNum> From<TNum>(TNum p1, TNum p2, TNum p3)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        => AABB<TNum>.From(p1, p2, p3);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AABB<TNum> From<TNum>(TNum p1, TNum p2, TNum p3, TNum p4)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        => AABB<TNum>.From(p1, p2, p3, p4);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AABB<TNum> From<TNum>(params TNum[] pts)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        => AABB<TNum>.From(pts);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AABB<TNum> Around<TNum>(TNum center, TNum size)
        where TNum : unmanaged, IFloatingPointIeee754<TNum> =>
        AABB<TNum>.Around(center, size);
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AABB<TNum> Combine<TNum>(IEnumerable<AABB<TNum>> select) 
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    =>AABB<TNum>.Combine(select);

    [Pure]
    public static TNum GetArea<TNum>(this AABB<Vector2<TNum>> aabb)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var size = aabb.Size;
        return Vector2<TNum>.IsFinite(size)
            ? TNum.Abs(size.X * size.Y)
            : TNum.NaN;
    }

    [Pure]
    public static TNum GetVolume<TNum>(this AABB<Vector3<TNum>> aabb)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var size = aabb.Size;
        return Vector3<TNum>.IsFinite(size)
            ? TNum.Abs(size.X * size.Y * size.Z)
            : TNum.NaN;
    }

    [Pure]
    public static TNum GetArea<TNum>(this AABB<Vector3<TNum>> aabb)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var size = aabb.Size;
        return Vector3<TNum>.IsFinite(size)
            ? size.YZX.Dot(size) * Numbers<TNum>.Two
            : TNum.NaN;
    }

    [Pure]
    public static IndexedMesh<TNum> Tessellate<TNum>(this AABB<Vector3<TNum>> box)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        => new(Vertices(box), Indices());

    [Pure]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public static TriangleIndexer[] Indices() => TesselationIndices[..];

    private static readonly TriangleIndexer[] TesselationIndices =
    [
        new(0b010, 0b110, 0b000), new(0b110, 0b100, 0b000),
        new(0b111, 0b011, 0b001), new(0b101, 0b111, 0b001),
        new(0b001, 0b011, 0b000), new(0b011, 0b010, 0b000),
        new(0b110, 0b111, 0b100), new(0b111, 0b101, 0b100),
        new(0b100, 0b101, 0b000), new(0b101, 0b001, 0b000),
        new(0b011, 0b111, 0b010), new(0b111, 0b110, 0b010)
    ];

    [Pure]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public static Vector3<TNum>[] Vertices<TNum>(AABB<Vector3<TNum>> box)
        where TNum : unmanaged, IFloatingPointIeee754<TNum> =>
    [
        box.Min, // 000
        new(box.Min.X, box.Min.Y, box.Max.Z), // 001
        new(box.Min.X, box.Max.Y, box.Min.Z), // 010
        new(box.Min.X, box.Max.Y, box.Max.Z), // 011
        new(box.Max.X, box.Min.Y, box.Min.Z), // 100
        new(box.Max.X, box.Min.Y, box.Max.Z), // 101
        new(box.Max.X, box.Max.Y, box.Min.Z), // 110
        box.Max // 111
    ];
}