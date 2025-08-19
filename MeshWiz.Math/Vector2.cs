using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Vector2<TNum> : IVector2<Vector2<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public static Vector2<TNum> Zero { [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]get; }= new(TNum.Zero, TNum.Zero);
    public static Vector2<TNum> One { [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]get; }= new(TNum.One, TNum.One);
    public static Vector2<TNum> NaN { [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]get; }= new(TNum.NaN, TNum.NaN);
    public Vector2<TNum> YX => new(Y, X);

    public Vector2<TOther> To<TOther>() where TOther : unmanaged, IFloatingPointIeee754<TOther>
        => new(TOther.CreateTruncating(X), TOther.CreateTruncating(Y));
    
    public readonly TNum X, Y;
    public static unsafe int ByteSize => sizeof(TNum) * 2;
    public int Count => 2;
    public Vector2<TNum> Normalized => this / Length;
    public TNum AlignedSquareVolume => X * Y;
    public static Vector2<TNum> FromXY(TNum x, TNum y) => new(x, y);
    public static Vector2<TNum> FromComponents<TList>(TList components)
        where TList : IReadOnlyList<TNum>
        =>new(components[0], components[1]);
    
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TNum> FromComponents<TList,TOtherNum>(TList components)
        where TList : IReadOnlyList<TOtherNum>
        where TOtherNum : INumber<TOtherNum>
        =>new(TNum.CreateTruncating(components[0]), TNum.CreateTruncating(components[1]));

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

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TNum> operator +(Vector2<TNum> left, Vector2<TNum> right)
        => new(left.X + right.X, left.Y + right.Y);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TNum> operator -(Vector2<TNum> left, Vector2<TNum> right)
        => new(left.X - right.X, left.Y - right.Y);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TNum> operator -(Vector2<TNum> vec) => new(-vec.X, -vec.Y);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TNum operator *(Vector2<TNum> left, Vector2<TNum> right)
        => left.X * right.X + left.Y * right.Y;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TNum> operator *(Vector2<TNum> vec, TNum scalar)
        => new(x: vec.X * scalar, y: vec.Y * scalar);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TNum> operator *(TNum scalar, Vector2<TNum> vec)
        => new(vec.X * scalar, vec.Y * scalar);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TNum> operator /(Vector2<TNum> vec, TNum divisor)
        => vec * (TNum.One / divisor);


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TNum operator ^(Vector2<TNum> left, Vector2<TNum> right)
        => left.X * right.Y - left.Y * right.X;

    [Pure, SuppressMessage("ReSharper", "CompareOfTNumsByEqualityOperator")]
    public static bool operator ==(Vector2<TNum> left, Vector2<TNum> right)
        => left.X == right.X && left.Y == right.Y;

    [Pure, SuppressMessage("ReSharper", "CompareOfTNumsByEqualityOperator")]
    public static bool operator !=(Vector2<TNum> left, Vector2<TNum> right)
        => left.X != right.X || left.Y != right.Y;

    #endregion

    #region functions

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2<TNum> Add(Vector2<TNum> other)
        => this + other;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2<TNum> Subtract(Vector2<TNum> other)
        => this - other;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2<TNum> Scale(TNum scalar)
        => this * scalar;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2<TNum> Divide(TNum divisor)
        => this / divisor;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum Dot(Vector2<TNum> other) => this * other;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum DistanceTo(Vector2<TNum> other) => (this - other).Length;
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum SquaredDistanceTo(Vector2<TNum> other) => (this-other).SquaredLength;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum Cross(Vector2<TNum> other) => this ^ other;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CrossSign(Vector2<TNum> other) => Cross(other).EpsilonTruncatingSign();

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsParallelTo(Vector2<TNum> other, TNum tolerance) 
        => TNum.Abs(Cross(other))<tolerance;

    public static Vector2<TNum> ExactLerp(Vector2<TNum> from, Vector2<TNum> toward, TNum exactDistance) 
        => (toward - from).Normalized * exactDistance + from;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsParallelTo(Vector2<TNum> other)
    =>IsParallelTo(other, TNum.Epsilon);

    #endregion

    #endregion

    #region functions

    #region general

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Vector2<TNum> other)
        => this == other;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(Vector2<TNum> other)
        => SquaredLength.CompareTo(other.SquaredLength);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ReadOnlySpan<TNum> AsSpan()
    {
        fixed (TNum* ptr = &X) return new ReadOnlySpan<TNum>(ptr, 2);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? other)
        => other is Vector2<TNum> vec && vec == this;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(X, Y);

    #endregion

    #endregion

    #region IReadOnlyList

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

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out TNum x, out TNum y)
    {
        x = X;
        y = Y;
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TNum> Lerp(Vector2<TNum> from, Vector2<TNum> to, TNum normalDistance)
    =>(to-from)*normalDistance+from;
    
    public static Vector2<TNum> SineLerp(Vector2<TNum> from, Vector2<TNum> to, TNum normalDistance)
    {
        var two = TNum.CreateTruncating(2);
        normalDistance = normalDistance.Wrap(TNum.Zero, two);
        var sineDistance= TNum.Sin(normalDistance * TNum.Pi / two);
        sineDistance = TNum.Clamp(sineDistance, TNum.Zero, TNum.One);
        return Lerp(from, to, sineDistance);
    }

    [SuppressMessage("ReSharper", "UseStringInterpolation")]
    public override string ToString()
        => $"{nameof(X)}: {X:F3}, {nameof(Y)}: {Y:F3}";

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNaN(Vector2<TNum> vec)
        => TNum.IsNaN(vec.X)&&TNum.IsNaN(vec.Y);
    
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsApprox(Vector2<TNum> other, TNum squareTolerance) => SquaredDistanceTo(other) < squareTolerance;
    
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsApprox(Vector2<TNum> other)=>SquaredDistanceTo(other)<=TNum.Epsilon;

    public Line<Vector2<TNum>, TNum> LineTo(Vector2<TNum> end) => new(this, end);

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        FormattableString formattable = $"{nameof(X)}: {X}, {nameof(Y)}: {Y}";
        return formattable.ToString(formatProvider);
    }
}