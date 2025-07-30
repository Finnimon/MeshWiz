using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Vector4<TNum> : IFloatingVector<Vector4<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly TNum X, Y, Z, W;

    public static unsafe int ByteSize => sizeof(TNum) * 4;
    public int Count => 4;
    static uint IVector<Vector4<TNum>, TNum>.Dimensions => 4;
    public Vector4<TNum> Normalized => this / Length;

    [Pure] public uint Dimensions => 4;
    [Pure] public TNum Length => TNum.Sqrt(SquaredLength);

    [Pure]
    public TNum SquaredLength
        => X * X + Y * Y + Z * Z + W * W;

    public Vector3<TNum> XYZ => new(X, Y, Z);


    public Vector4(TNum x, TNum y, TNum z, TNum w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector4(Vector3<TNum> xyz, TNum w) => ((X, Y, Z), W) = (xyz, w);

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> FromXYZW(TNum x, TNum y, TNum z, TNum w)
        => new(x, y, z, w);
    public Vector4<TOtherNum> To<TOtherNum>()
        where TOtherNum : unmanaged, IFloatingPointIeee754<TOtherNum>
        =>new(
            TOtherNum.CreateTruncating(X),
            TOtherNum.CreateTruncating(Y),
            TOtherNum.CreateTruncating(Z),
            TOtherNum.CreateTruncating(W)
        );
    
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> FromComponents<TList>(TList components)
        where TList : IReadOnlyList<TNum>
        =>new(components[0], components[1],components[2],components[3]);

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> FromComponents<TList,TOtherNum>(TList components)
        where TList : IReadOnlyList<TOtherNum>
        where TOtherNum : INumber<TOtherNum>
        =>new(TNum.CreateTruncating(components[0]), 
            TNum.CreateTruncating(components[1]),
            TNum.CreateTruncating(components[2]),
            TNum.CreateTruncating(components[3]));

    public static Vector4<TNum> Zero => new(TNum.Zero, TNum.Zero, TNum.Zero, TNum.Zero);
    public static Vector4<TNum> One => new(TNum.One, TNum.One, TNum.One, TNum.One);
    public static Vector4<TNum> NaN => new(TNum.NaN, TNum.NaN, TNum.NaN, TNum.NaN);


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> operator +(Vector4<TNum> left, Vector4<TNum> right)
        => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> operator -(Vector4<TNum> left, Vector4<TNum> right)
        => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> operator -(Vector4<TNum> vec)=>new(-vec.X,-vec.Y,-vec.Z,-vec.W);
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TNum operator *(Vector4<TNum> left, Vector4<TNum> right)
        => left.X * right.X + left.Y * right.Y + left.Z * right.Z + right.W * right.W;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> operator *(Vector4<TNum> vec, TNum scalar)
        => new(x: vec.X * scalar, y: vec.Y * scalar, z: vec.Z * scalar, vec.W * scalar);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> operator *(TNum scalar, Vector4<TNum> vec)
        => new(vec.X * scalar, vec.Y * scalar, vec.Z * scalar, vec.W * scalar);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> operator /(Vector4<TNum> vec, TNum divisor)
        => vec * (TNum.One / divisor);


    [Pure, SuppressMessage("ReSharper", "CompareOfTNumsByEqualityOperator")]
    public static bool operator ==(Vector4<TNum> left, Vector4<TNum> right)
        => left.X == right.X && left.Y == right.Y && left.Z == right.Z;

    [Pure, SuppressMessage("ReSharper", "CompareOfTNumsByEqualityOperator")]
    public static bool operator !=(Vector4<TNum> left, Vector4<TNum> right)
        => left.X != right.X || left.Y != right.Y || left.Z != right.Z;


    #region functions

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector4<TNum> Add(Vector4<TNum> other)
        => this + other;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector4<TNum> Subtract(Vector4<TNum> other)
        => this - other;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector4<TNum> Scale(TNum scalar)
        => this * scalar;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector4<TNum> Divide(TNum divisor)
        => this / divisor;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum Dot(Vector4<TNum> other) => this * other;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum DistanceTo(Vector4<TNum> other) => (this - other).Length;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum SquaredDistanceTo(Vector4<TNum> other) => (this-other).SquaredLength;
    
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsParallelTo(Vector4<TNum> other, TNum tolerance) 
        => tolerance>=TNum.Abs(Normalized * other.Normalized)-TNum.One;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsParallelTo(Vector4<TNum> other)
        =>IsParallelTo(other, TNum.Epsilon);
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Vector4<TNum> other)
        => this == other;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(Vector4<TNum> other)
        => SquaredLength.CompareTo(other.SquaredLength);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ReadOnlySpan<TNum> AsSpan()
    {
        fixed (TNum* ptr = &X) return new ReadOnlySpan<TNum>(ptr, 4);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? other)
        => other is Vector4<TNum> vec && vec == this;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    [Pure]
    public unsafe TNum this[int index]
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (index.InsideInclusiveRange(0, 2))
                fixed (TNum* ptr = &X)
                    return ptr[index];
            throw new IndexOutOfRangeException();
        }
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerator<TNum> GetEnumerator()
    {
        yield return X;
        yield return Y;
        yield return Z;
    }

    [SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
    IEnumerator IEnumerable.GetEnumerator()
    {
        yield return X;
        yield return Y;
        yield return Z;
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out TNum x, out TNum y, out TNum z, out TNum w)
    {
        x = X;
        y = Y;
        z = Z;
        w = W;
    }

    #endregion

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> Lerp(Vector4<TNum> from, Vector4<TNum> to, TNum normalDistance)
        => (to - from) * normalDistance + from;

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> CosineLerp(Vector4<TNum> from, Vector4<TNum> to, TNum normalDistance)
    {
        var two = TNum.CreateTruncating(2);
        normalDistance = normalDistance.Wrap(TNum.Zero, two);
        var cosDistance = (-TNum.Cos(normalDistance * TNum.Pi) + TNum.One)/two;
        return Lerp(from, to, cosDistance);
    }


    [SuppressMessage("ReSharper", "UseStringInterpolation")]
    public override string ToString()
        => string.Format("{{X:{0:F4} Y:{1:F4} Z:{2:F4} W:{3:F4}}}", X, Y, Z, W);
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNaN(Vector4<TNum> vec)
        => TNum.IsNaN(vec.X)||TNum.IsNaN(vec.Y)||TNum.IsNaN(vec.Z)||TNum.IsNaN(vec.W);
    
        
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsApprox(Vector4<TNum> other, TNum squareTolerance) => SquaredDistanceTo(other) < squareTolerance;
    
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsApprox(Vector4<TNum> other)=>SquaredDistanceTo(other)<=TNum.Epsilon;

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Line<Vector4<TNum>, TNum> LineTo(Vector4<TNum> end) => new(this, end);
}