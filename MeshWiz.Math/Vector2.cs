using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.InteropServices;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Vector2<TNum> : IVector2<Vector2<TNum>, TNum>
    where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum>
{
    public static Vector2<TNum> Zero => new(TNum.Zero, TNum.Zero);
    public static Vector2<TNum> One => new(TNum.One, TNum.One);
    public static Vector2<TNum> NaN => new(TNum.NaN, TNum.NaN);

    public Vector2<TNum> YX => new(Y, X);
    public readonly TNum X, Y;
    public static unsafe int ByteSize => sizeof(TNum) * 2;
    public int Count => 2;
    public Vector2<TNum> Normalized => this / Length;
    public TNum AlignedSquareVolume => X * Y;
    public static Vector2<TNum> FromXY(TNum x, TNum y) => new(x, y);

    public Vector2(TNum x, TNum y)
    {
        X = x;
        Y = y;
    }

    [Pure] public uint Dimensions => 2;
    [Pure] public TNum Length => TNum.Sqrt(SquaredLength);

    [Pure]
    public TNum SquaredLength
        => X * X + Y * Y;


    #region arithmetic

    #region operators

    [Pure]
    public static Vector2<TNum> operator +(in Vector2<TNum> left, in Vector2<TNum> right)
        => new(left.X + right.X, left.Y + right.Y);

    [Pure]
    public static Vector2<TNum> operator -(in Vector2<TNum> left, in Vector2<TNum> right)
        => new(left.X - right.X, left.Y - right.Y);

    [Pure]
    public static TNum operator *(in Vector2<TNum> left, in Vector2<TNum> right)
        => left.X * right.X + left.Y * right.Y;

    [Pure]
    public static Vector2<TNum> operator *(in Vector2<TNum> vec, TNum scalar)
        => new(x: vec.X * scalar, y: vec.Y * scalar);

    [Pure]
    public static Vector2<TNum> operator *(TNum scalar, in Vector2<TNum> vec)
        => new(vec.X * scalar, vec.Y * scalar);

    [Pure]
    public static Vector2<TNum> operator /(in Vector2<TNum> vec, TNum divisor)
        => vec * (TNum.One / divisor);


    [Pure]
    public static TNum operator ^(in Vector2<TNum> left, in Vector2<TNum> right)
        => left.X * right.Y - left.Y * right.X;

    [Pure, SuppressMessage("ReSharper", "CompareOfTNumsByEqualityOperator")]
    public static bool operator ==(in Vector2<TNum> left, in Vector2<TNum> right)
        => left.X == right.X && left.Y == right.Y;

    [Pure, SuppressMessage("ReSharper", "CompareOfTNumsByEqualityOperator")]
    public static bool operator !=(in Vector2<TNum> left, in Vector2<TNum> right)
        => left.X != right.X || left.Y != right.Y;

    #endregion

    #region functions

    [Pure]
    public Vector2<TNum> Add(in Vector2<TNum> other)
        => this + other;

    [Pure]
    public Vector2<TNum> Subtract(in Vector2<TNum> other)
        => this - other;

    [Pure]
    public Vector2<TNum> Scale(in TNum scalar)
        => this * scalar;

    [Pure]
    public Vector2<TNum> Divide(in TNum divisor)
        => this / divisor;


    [Pure]
    public TNum Dot(in Vector2<TNum> other) => this * other;

    [Pure]
    public TNum Distance(in Vector2<TNum> other) => (this - other).Length;

    [Pure]
    public TNum Cross(in Vector2<TNum> other) => this ^ other;

    [Pure]
    public int CrossSign(in Vector2<TNum> other) => TNum.Sign(Cross(in other));

    #endregion

    #endregion

    #region functions

    #region general

    [Pure]
    public bool Equals(Vector2<TNum> other)
        => this == other;

    [Pure]
    public int CompareTo(Vector2<TNum> other)
        => SquaredLength.CompareTo(other.SquaredLength);

    [Pure]
    public unsafe ReadOnlySpan<TNum> AsSpan()
    {
        fixed (TNum* ptr = &X) return new ReadOnlySpan<TNum>(ptr, 2);
    }

    [Pure]
    public override bool Equals(object? other)
        => other is Vector2<TNum> vec && vec == this;

    [Pure]
    public override int GetHashCode() => HashCode.Combine(X, Y);

    #endregion

    #endregion

    #region IReadOnlyList

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

    IEnumerator<TNum> IEnumerable<TNum>.GetEnumerator()
    {
        yield return X;
        yield return Y;
    }

    [SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
    IEnumerator IEnumerable.GetEnumerator()
    {
        yield return X;
        yield return Y;
    }

    #endregion

    public static explicit operator Vector3<TNum>(Vector2<TNum> vec)
        => new(vec.X, vec.Y, TNum.Zero);

    public void Deconstruct(out TNum x, out TNum y)
    {
        x = X;
        y = Y;
    }
    public static Vector2<TNum> Lerp(in Vector2<TNum> from, in Vector2<TNum> to, TNum normalDistance)
    =>(to-from)*normalDistance+from;

    [SuppressMessage("ReSharper", "UseStringInterpolation")]
    public override string ToString()
        => string.Format("{{X:{0:F3} Y:{1:F3}}}", X, Y);
}