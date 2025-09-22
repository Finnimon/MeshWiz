using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

/// <summary>
/// Axis aligned Bounding Box
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct AABB<TNum>(TNum min, TNum max)
    : IEqualityOperators<AABB<TNum>, AABB<TNum>, bool>,
        IEquatable<AABB<TNum>>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public static readonly AABB<TNum> Empty = new(TNum.PositiveInfinity, TNum.NegativeInfinity);
    public readonly TNum Min = min, Max = max;
    public TNum Size => Max - Min;
    public TNum Center => Min + Size / Numbers<TNum>.Two;
    private AABB(TNum p) : this(p, p) { }

    public AABB<TNum> CombineWith(TNum p)
    {
        var min = TNum.Min(p, Min);
        var max = TNum.Max(p, Max);
        return new AABB<TNum>(min, max);
    }

    public AABB<TNum> CombineWith(TNum p1, TNum p2)
    {
        var min = TNum.Min(p1, Min);
        min = TNum.Min(p2, min);
        var max = TNum.Max(p1, Max);
        max = TNum.Max(p2, max);
        return new AABB<TNum>(min, max);
    }

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

    public AABB<TNum> CombineWith(AABB<TNum> other)
        => new(TNum.Min(Min, other.Min), TNum.Max(Max, other.Max));

    public AABB<TNum> CombineWith(AABB<TNum> aabb1, AABB<TNum> aabb2)
        => new(TNum.Min(TNum.Min(Min, aabb1.Min), aabb2.Min),
            TNum.Max(TNum.Max(Max, aabb1.Max), aabb2.Max));


    public static AABB<TNum> Combine(AABB<TNum> a, AABB<TNum> b)
        => a.CombineWith(b);

    public static AABB<TNum> Combine(AABB<TNum> a, AABB<TNum> b, AABB<TNum> c)
        => a.CombineWith(b, c);

    public static AABB<TNum> Combine(AABB<TNum> a, TNum p)
        => a.CombineWith(p);

    public static AABB<TNum> Combine(AABB<TNum> a, TNum p1, TNum p2)
        => a.CombineWith(p1, p2);

    public static AABB<TNum> Combine(AABB<TNum> a, TNum p1, TNum p2, TNum p3)
        => a.CombineWith(p1, p2, p3);

    public static AABB<TNum> Combine(AABB<TNum> a, TNum p1, TNum p2, TNum p3, TNum p4)
        => a.CombineWith(p1, p2, p3, p4);


    public static AABB<TNum> From(TNum p)
        => new(p);

    public static AABB<TNum> From(TNum p1, TNum p2)
    {
        var min = TNum.Min(p1, p2);
        var max = TNum.Max(p1, p2);
        return new(min, max);
    }
    
    public static AABB<TNum> Around(TNum center, TNum size)
    {
        size/=Numbers<TNum>.Two;
        return From(center-size,center+size);
    }
    

    public static AABB<TNum> From(TNum p1, TNum p2, TNum p3)
    {
        var min = TNum.Min(p1, p2);
        var max = TNum.Max(p1, p2);
        min = TNum.Min(min, p3);
        max = TNum.Max(max, p3);
        return new AABB<TNum>(min, max);
    }

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


    public static AABB<TNum> From(params TNum[] pts)
    {
        var min = TNum.PositiveInfinity;
        var max = TNum.NegativeInfinity;
        foreach (var p in pts)
        {
            min = TNum.Min(p, min);
            max = TNum.Max(p, max);
        }

        return new(min, max);
    }

    public bool Contains(TNum p) => Clamp(p) == p;
    public bool Contains(TNum p, TNum epsilon) => TNum.Abs(Clamp(p) - p) < epsilon;

    public bool IntersectsWith(AABB<TNum> other) => Contains(TNum.Clamp(Center, other.Min, other.Max));


    /// <inheritdoc />
    public static bool operator ==(AABB<TNum> left, AABB<TNum> right)
        => left.Equals(right);

    /// <inheritdoc />
    public static bool operator !=(AABB<TNum> left, AABB<TNum> right)
        => !left.Equals(right);

    /// <inheritdoc />
    public bool Equals(AABB<TNum> other)
        => Min == other.Min && Max == other.Max;


    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is AABB<TNum> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Min, Max);

    public TNum DistanceTo(TNum p)
        => TNum.Abs(p - Clamp(p));


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum Clamp(TNum value) => TNum.Clamp(value, Min, Max);
}

public static class AABB
{
    public static TNum GetArea<TNum>(this AABB<Vector2<TNum>> aabb)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var size = aabb.Size;
        return Vector2<TNum>.IsFinite(size)
            ? TNum.Abs(size.X * size.Y)
            : TNum.NaN;
    }

    public static TNum GetVolume<TNum>(this AABB<Vector3<TNum>> aabb)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var size = aabb.Size;
        return Vector3<TNum>.IsFinite(size)
            ? TNum.Abs(size.X * size.Y * size.Z)
            : TNum.NaN;
    }

    public static TNum GetArea<TNum>(this AABB<Vector3<TNum>> aabb)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var size = aabb.Size;
        return Vector3<TNum>.IsFinite(size)
            ? size.YZX.Dot(size) * Numbers<TNum>.Two
            : TNum.NaN;
    }

    public static IndexedMesh<TNum> Tessellate<TNum>(this AABB<Vector3<TNum>> box)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        => new(Vertices(box), Indices());

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