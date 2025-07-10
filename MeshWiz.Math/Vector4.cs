using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
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

    public static Vector4<TNum> FromXYZW(TNum x, TNum y, TNum z, TNum w)
        => new(x, y, z, w);

    public static Vector4<TNum> FromComponents(TNum[] components) =>
        new(components[0], components[1], components[2], components[3]);

    public static Vector4<TNum> FromComponents(ReadOnlySpan<TNum> components) =>
        new(components[0], components[1], components[2], components[3]);

    public static Vector4<TNum> Zero => new(TNum.Zero, TNum.Zero, TNum.Zero, TNum.Zero);
    public static Vector4<TNum> One => new(TNum.One, TNum.One, TNum.One, TNum.One);
    public static Vector4<TNum> NaN => new(TNum.NaN, TNum.NaN, TNum.NaN, TNum.NaN);


    [Pure]
    public static Vector4<TNum> operator +(in Vector4<TNum> left, in Vector4<TNum> right)
        => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);

    [Pure]
    public static Vector4<TNum> operator -(in Vector4<TNum> left, in Vector4<TNum> right)
        => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);
    [Pure]
    public static Vector4<TNum> operator -(in Vector4<TNum> vec)=>new(-vec.X,-vec.Y,-vec.Z,-vec.W);
    [Pure]
    public static TNum operator *(in Vector4<TNum> left, in Vector4<TNum> right)
        => left.X * right.X + left.Y * right.Y + left.Z * right.Z + right.W * right.W;

    [Pure]
    public static Vector4<TNum> operator *(in Vector4<TNum> vec, TNum scalar)
        => new(x: vec.X * scalar, y: vec.Y * scalar, z: vec.Z * scalar, vec.W * scalar);

    [Pure]
    public static Vector4<TNum> operator *(TNum scalar, in Vector4<TNum> vec)
        => new(vec.X * scalar, vec.Y * scalar, vec.Z * scalar, vec.W * scalar);

    [Pure]
    public static Vector4<TNum> operator /(in Vector4<TNum> vec, TNum divisor)
        => vec * (TNum.One / divisor);


    [Pure, SuppressMessage("ReSharper", "CompareOfTNumsByEqualityOperator")]
    public static bool operator ==(in Vector4<TNum> left, in Vector4<TNum> right)
        => left.X == right.X && left.Y == right.Y && left.Z == right.Z;

    [Pure, SuppressMessage("ReSharper", "CompareOfTNumsByEqualityOperator")]
    public static bool operator !=(in Vector4<TNum> left, in Vector4<TNum> right)
        => left.X != right.X || left.Y != right.Y || left.Z != right.Z;


    #region functions

    [Pure]
    public Vector4<TNum> Add(in Vector4<TNum> other)
        => this + other;

    [Pure]
    public Vector4<TNum> Subtract(in Vector4<TNum> other)
        => this - other;

    [Pure]
    public Vector4<TNum> Scale(in TNum scalar)
        => this * scalar;

    [Pure]
    public Vector4<TNum> Divide(in TNum divisor)
        => this / divisor;


    [Pure]
    public TNum Dot(in Vector4<TNum> other) => this * other;

    [Pure]
    public TNum Distance(in Vector4<TNum> other) => (this - other).Length;

    
    [Pure]
    public bool IsParallelTo(in Vector4<TNum> other, TNum tolerance) 
        => tolerance>=TNum.Abs(Normalized * other.Normalized);

    [Pure]
    public bool IsParallelTo(in Vector4<TNum> other)
        =>IsParallelTo(other, TNum.Epsilon);
    [Pure]
    public bool Equals(Vector4<TNum> other)
        => this == other;

    [Pure]
    public int CompareTo(Vector4<TNum> other)
        => SquaredLength.CompareTo(other.SquaredLength);

    [Pure]
    public unsafe ReadOnlySpan<TNum> AsSpan()
    {
        fixed (TNum* ptr = &X) return new ReadOnlySpan<TNum>(ptr, 4);
    }

    [Pure]
    public override bool Equals(object? other)
        => other is Vector4<TNum> vec && vec == this;

    [Pure]
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    [Pure]
    public unsafe TNum this[int index]
    {
        [Pure]
        get
        {
            if (index.InsideInclusiveRange(0, 2))
                fixed (TNum* ptr = &X)
                    return ptr[index];
            throw new IndexOutOfRangeException();
        }
    }

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

    public void Deconstruct(out TNum x, out TNum y, out TNum z, out TNum w)
    {
        x = X;
        y = Y;
        z = Z;
        w = W;
    }

    #endregion

    public static Vector4<TNum> Lerp(in Vector4<TNum> from, in Vector4<TNum> to, TNum normalDistance)
        => (to - from) * normalDistance + from;

    public static Vector4<TNum> SineLerp(in Vector4<TNum> from, in Vector4<TNum> to, TNum normalDistance)
    {
        var two = TNum.CreateTruncating(2);
        normalDistance = normalDistance.Wrap(TNum.Zero, two);
        var sineDistance= TNum.Sin(normalDistance * TNum.Pi / two);
        sineDistance = TNum.Clamp(sineDistance, TNum.Zero, TNum.One);
        return Lerp(from, to, sineDistance);
    }


    [SuppressMessage("ReSharper", "UseStringInterpolation")]
    public override string ToString()
        => string.Format("{{X:{0:F4} Y:{1:F4} Z:{2:F4} W:{3:F4}}}", X, Y, Z, W);
}