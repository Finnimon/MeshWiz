using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct BBox3<TNum> : IBody<TNum>, IEquatable<BBox3<TNum>>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public static readonly BBox3<TNum> NegativeInfinity = new(
        new(TNum.PositiveInfinity, TNum.PositiveInfinity, TNum.PositiveInfinity),
        new(TNum.NegativeInfinity, TNum.NegativeInfinity, TNum.NegativeInfinity));

    public readonly Vector3<TNum> Min, Max;
    public BBox3<TNum> BBox => this;
    public Vector3<TNum> Centroid => (Min + Max) / TNum.CreateTruncating(2);
    public Vector3<TNum> Size => Max - Min;
    private Vector3<TNum> Diagonal => Max - Min;

    public TNum Volume => Diagonal.AlignedCuboidVolume;

    public TNum SurfaceArea => CalculateSurfaceArea();

    public ISurface<Vector3<TNum>, TNum> Surface => this;
    
    public BBox3(Vector3<TNum> min, Vector3<TNum> max)
    {
        Min = min;
        Max = max;
    }

    private TNum CalculateSurfaceArea()
    {
        var d = Diagonal;
        return TNum.CreateTruncating(2) * d.YZX.Dot(d);
    }

    // return
    // [
    //     new(p010, p110, p000), new(p110, p100, p000),
    // new(p111, p011, p001), new(p101, p111, p001),
    //
    // new(p001, p011, p000), new(p011, p010, p000),
    //
    // new(p110, p111, p100), new(p111, p101, p100),
    //
    // new(p100, p101, p000), new(p101, p001, p000),
    //
    // new(p011, p111, p010), new(p111, p110, p010),
    // ];
    public IMesh<TNum> Tessellate()
    {
        
        return new IndexedMesh<TNum>(Vertices, Indices);
    }
    
    private Vector3<TNum>[] Vertices =>
    [
        Min,                        // 000
        new(Min.X, Min.Y, Max.Z),   // 001
        new(Min.X, Max.Y, Min.Z),   // 010
        new(Min.X, Max.Y, Max.Z),   // 011
        new(Max.X, Min.Y, Min.Z),   // 100
        new(Max.X, Min.Y, Max.Z),   // 101
        new(Max.X, Max.Y, Min.Z),   // 110
        Max                         // 111
    ];
    
    private static TriangleIndexer[] Indices=>[
            new(0b010, 0b110, 0b000), new(0b110, 0b100, 0b000),
            new(0b111, 0b011, 0b001), new(0b101, 0b111, 0b001),
            new(0b001, 0b011, 0b000), new(0b011, 0b010, 0b000),
            new(0b110, 0b111, 0b100), new(0b111, 0b101, 0b100),
            new(0b100, 0b101, 0b000), new(0b101, 0b001, 0b000),
            new(0b011, 0b111, 0b010), new(0b111, 0b110, 0b010)
        ];


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BBox3<TNum> Combine(in BBox3<TNum> a, in BBox3<TNum> b)
        => a.CombineWith(b);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BBox3<TNum> IncludePoint(BBox3<TNum> box, in Vector3<TNum> point)
        => box.CombineWith(point);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BBox3<TNum> CombineWith(in Vector3<TNum> p) 
        => new(Vector3<TNum>.Min(Min,p), Vector3<TNum>.Max(Max,p));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BBox3<TNum> CombineWith(in BBox3<TNum> o)
        => new(Vector3<TNum>.Min(Min,o.Min), Vector3<TNum>.Max(Max,o.Max));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BBox3<TNum> FromPoint(Vector3<TNum> point) => new(point, point);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(in BBox3<TNum> a, in BBox3<TNum> b) => a.Min != b.Min || a.Max != b.Max;
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(in BBox3<TNum> a, in BBox3<TNum> b) => a.Min == b.Min && a.Max == b.Max;



    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is not BBox3<TNum> box) return false;
        return this == box;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(BBox3<TNum> other) => this == other;
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(Min, Max);
}