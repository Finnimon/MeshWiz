using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Vector2<TNum>(TNum x, TNum y) : IVector2<Vector2<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public static Vector2<TNum> Zero { [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]get; }= new(TNum.Zero, TNum.Zero);

    public Vector2<TNum> Right => new(Y, -X);
    public Vector2<TNum> Left => new(-Y, X);

    public static Vector2<TNum> One { [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]get; }= new(TNum.One, TNum.One);
    public static Vector2<TNum> NaN { [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]get; }= new(TNum.NaN, TNum.NaN);

    /// <inheritdoc />
    public static Vector2<TNum> NegativeInfinity { get; } = new(TNum.NegativeInfinity);

    /// <inheritdoc />
    public static Vector2<TNum> NegativeZero { get; } = new(TNum.NegativeZero);

    /// <inheritdoc />
    public static Vector2<TNum> PositiveInfinity { get; } = new(TNum.PositiveInfinity);
    public Vector2<TNum> YX => new(Y, X);

    /// <inheritdoc />
    public static Vector2<TNum> operator /(TNum l, Vector2<TNum> r)
        => new(l / r.X, l / r.Y);
    public TNum Sum => X + Y;

    public Vector2<TOther> To<TOther>() where TOther : unmanaged, IFloatingPointIeee754<TOther>
        => new(TOther.CreateTruncating(X), TOther.CreateTruncating(Y));
    
    public readonly TNum X = x, Y = y;
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
        where TOtherNum : INumberBase<TOtherNum>
        =>new(TNum.CreateTruncating(components[0]), TNum.CreateTruncating(components[1]));

    /// <inheritdoc />
    public static Vector2<TNum> FromComponentsConstrained<TList, TOtherNum>(TList components) 
        where TList : IReadOnlyList<TOtherNum> 
        where TOtherNum : INumberBase<TOtherNum>
    {
        var x=components.Count>0?TNum.CreateTruncating(components[0]):TNum.Zero;
        var y=components.Count>1?TNum.CreateTruncating(components[1]):TNum.Zero;
        return new(x, y);
    }

    /// <inheritdoc />
    public static Vector2<TNum> FromComponentsConstrained<TList>(TList components) where TList : IReadOnlyList<TNum>
    {
        var x=components.Count>0?components[0]:TNum.Zero;
        var y=components.Count>1?components[1]:TNum.Zero;
        return new(x, y);
    }

    /// <inheritdoc />
    public static Vector2<TNum> FromValue(TNum value)
        => new(value);

    /// <inheritdoc />
    public static Vector2<TNum> FromValue<TOtherNum>(TOtherNum other) where TOtherNum : INumberBase<TOtherNum>
        => new(TNum.CreateTruncating(other));

    private Vector2(TNum s) :this(s,s){ }

    [Pure] public static uint Dimensions => 2;
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

    // [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static TNum operator *(Vector2<TNum> left, Vector2<TNum> right)
    //     => left.X * right.X + left.Y * right.Y;

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
    public TNum Dot(Vector2<TNum> other) => X * other.X + Y * other.Y;

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

    /// <inheritdoc />
    public int CompareTo(object? obj)
        => obj is Vector2<TNum> v ? CompareTo(v) : 1;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage("ReSharper", "EqualExpressionComparison")]
    // [SuppressMessage("ReSharper", "EqualExpressionComparison")]
    public static bool IsNaN(Vector2<TNum> vec)
#pragma warning disable CS1718
        => vec != vec;
#pragma warning restore CS1718
    
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsApprox(Vector2<TNum> other, TNum squareTolerance) => SquaredDistanceTo(other) < squareTolerance;
    
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsApprox(Vector2<TNum> other)=>SquaredDistanceTo(other)<=TNum.Epsilon;

    public Line<Vector2<TNum>, TNum> LineTo(Vector2<TNum> end) => new(this, end);



    /// <inheritdoc />
    public static Vector2<TNum> operator %(Vector2<TNum> l, Vector2<TNum> r)
        => new(l.X % r.X, l.Y % r.Y);

    /// <inheritdoc />
    public static Vector2<TNum> operator +(Vector2<TNum> v)
        => new(+v.X, +v.Y);

    /// <inheritdoc />
    public static Vector2<TNum> Pow(Vector2<TNum> x, Vector2<TNum> y)
        => new(TNum.Pow(x.X, y.X), TNum.Pow(x.Y, y.Y));
    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2<TNum> Abs(Vector2<TNum> v)
        => new(TNum.Abs(v.X), TNum.Abs(v.Y));

    /// <inheritdoc />
    public static bool IsCanonical(Vector2<TNum> value)
        => TNum.IsCanonical(value.Sum);

    /// <inheritdoc />
    public static bool IsComplexNumber(Vector2<TNum> v)
        => TNum.IsComplexNumber(v.Sum);

    /// <inheritdoc />
    public static bool IsEvenInteger(Vector2<TNum> v)
        => TNum.IsEvenInteger(v.Sum);

    /// <inheritdoc />
    public static bool IsFinite(Vector2<TNum> v)
        => TNum.IsFinite(v.Sum);

    /// <inheritdoc />
    public static bool IsImaginaryNumber(Vector2<TNum> value)
        => TNum.IsImaginaryNumber(value.Sum);

    /// <inheritdoc />
    public static bool IsInfinity(Vector2<TNum> value)
        => TNum.IsInfinity(value.Sum);

    /// <inheritdoc />
    public static bool IsInteger(Vector2<TNum> value)
        => TNum.IsInteger(value.Sum);
    /// <inheritdoc />
    public static Vector2<TNum> AdditiveIdentity => Zero;

    /// <inheritdoc />
    public static bool operator >(Vector2<TNum> left, Vector2<TNum> right)
        => left.SquaredLength > right.SquaredLength;

    /// <inheritdoc />
    public static bool operator >=(Vector2<TNum> left, Vector2<TNum> right)
        => left.SquaredLength >= right.SquaredLength;

    /// <inheritdoc />
    public static bool operator <(Vector2<TNum> left, Vector2<TNum> right)
        => left.SquaredLength < right.SquaredLength;

    /// <inheritdoc />
    public static bool operator <=(Vector2<TNum> left, Vector2<TNum> right)
        => left.SquaredLength <= right.SquaredLength;

    /// <inheritdoc />
    public static Vector2<TNum> operator --(Vector2<TNum> v)
        => v - One;

    /// <inheritdoc />
    public static Vector2<TNum> operator /(Vector2<TNum> l, Vector2<TNum> r)
        => new(l.X / r.X, l.Y / r.Y);

    /// <inheritdoc />
    public static Vector2<TNum> operator ++(Vector2<TNum> v)
        => v + One;

    /// <inheritdoc />
    public static Vector2<TNum> MultiplicativeIdentity => One;

    /// <inheritdoc />
    public static Vector2<TNum> E { get; } = new(TNum.E);

    /// <inheritdoc />
    public static Vector2<TNum> Pi { get; } = new(TNum.Pi);

    /// <inheritdoc />
    public static Vector2<TNum> Tau { get; } = new(TNum.Tau);
    
    /// <inheritdoc />
    public static Vector2<TNum> Epsilon { get; } = new(TNum.Epsilon);

    /// <inheritdoc />
    public static Vector2<TNum> Exp(Vector2<TNum> v)
        => new(TNum.Exp(v.X), TNum.Exp(v.Y));

    /// <inheritdoc />
    public static Vector2<TNum> Exp10(Vector2<TNum> v)
        => new(TNum.Exp10(v.X), TNum.Exp10(v.Y));

    /// <inheritdoc />
    public static Vector2<TNum> Exp2(Vector2<TNum> v)
        => new(TNum.Exp2(v.X), TNum.Exp2(v.Y));


    /// <inheritdoc />
    public static Vector2<TNum> NegativeOne { get; } = new(TNum.NegativeOne);


    /// <inheritdoc />
    public static Vector2<TNum> Round(Vector2<TNum> vec, int digits, MidpointRounding mode)
        => new(TNum.Round(vec.X, digits, mode),
            TNum.Round(vec.Y, digits, mode));


    /// <inheritdoc />
    public static Vector2<TNum> Acosh(Vector2<TNum> vec)
        => new(TNum.Acosh(vec.X), TNum.Acosh(vec.Y));

    /// <inheritdoc />
    public static Vector2<TNum> Asinh(Vector2<TNum> vec)
        => new(TNum.Asinh(vec.X), TNum.Asinh(vec.Y));

    /// <inheritdoc />
    public static Vector2<TNum> Atanh(Vector2<TNum> vec)
        => new(TNum.Atanh(vec.X), TNum.Atanh(vec.Y));

    /// <inheritdoc />
    public static Vector2<TNum> Cosh(Vector2<TNum> vec)
        => new(TNum.Cosh(vec.X), TNum.Cosh(vec.Y));


    /// <inheritdoc />
    public static Vector2<TNum> Sinh(Vector2<TNum> vec)
        => new(TNum.Sinh(vec.X), TNum.Sinh(vec.Y));


    /// <inheritdoc />
    public static Vector2<TNum> Tanh(Vector2<TNum> vec)
        => new(TNum.Tanh(vec.X), TNum.Tanh(vec.Y));

    /// <inheritdoc />
    public static Vector2<TNum> Log(Vector2<TNum> vec)
        => new(TNum.Log(vec.X), TNum.Log(vec.Y));

    /// <inheritdoc />
    public static Vector2<TNum> Log(Vector2<TNum> vec, Vector2<TNum> newBase)
        => new(TNum.Log(vec.X, newBase.X),
            TNum.Log(vec.Y, newBase.Y));

    /// <inheritdoc />
    public static Vector2<TNum> Log10(Vector2<TNum> vec)
        => new(TNum.Log10(vec.X), TNum.Log10(vec.Y));

    /// <inheritdoc />
    public static Vector2<TNum> Log2(Vector2<TNum> vec)
        => new(TNum.Log2(vec.X), TNum.Log2(vec.Y));


    /// <inheritdoc />
    public static Vector2<TNum> Cbrt(Vector2<TNum> vec)
        => new(TNum.Cbrt(vec.X), TNum.Cbrt(vec.Y));

    /// <inheritdoc />
    public static Vector2<TNum> Hypot(Vector2<TNum> x, Vector2<TNum> y)
        => new(TNum.Hypot(x.X, y.X), TNum.Hypot(x.Y, y.Y));


    /// <inheritdoc />
    public static Vector2<TNum> RootN(Vector2<TNum> vec, int n)
        => new(TNum.RootN(vec.X, n), TNum.RootN(vec.Y, n));


    /// <inheritdoc />
    public static Vector2<TNum> Sqrt(Vector2<TNum> vec)
        => new(TNum.Sqrt(vec.X), TNum.Sqrt(vec.Y));


    /// <inheritdoc />
    public static Vector2<TNum> Acos(Vector2<TNum> vec)
        => new(TNum.Acos(vec.X), TNum.Acos(vec.Y));

    /// <inheritdoc />
    public static Vector2<TNum> AcosPi(Vector2<TNum> vec)
        => new(TNum.AcosPi(vec.X), TNum.AcosPi(vec.Y));


    /// <inheritdoc />
    public static Vector2<TNum> Asin(Vector2<TNum> vec)
        => new(TNum.Asin(vec.X), TNum.Asin(vec.Y));


    /// <inheritdoc />
    public static Vector2<TNum> AsinPi(Vector2<TNum> vec)
        => new(TNum.AsinPi(vec.X), TNum.AsinPi(vec.Y));


    /// <inheritdoc />
    public static Vector2<TNum> Atan(Vector2<TNum> vec)
        => new(TNum.Atan(vec.X), TNum.Atan(vec.Y));


    /// <inheritdoc />
    public static Vector2<TNum> AtanPi(Vector2<TNum> vec)
        => new(TNum.AtanPi(vec.X), TNum.AtanPi(vec.Y));


    /// <inheritdoc />
    public static Vector2<TNum> Cos(Vector2<TNum> vec)
        => new(TNum.Cos(vec.X), TNum.Cos(vec.Y));

    /// <inheritdoc />
    public static Vector2<TNum> CosPi(Vector2<TNum> vec)
        => new(TNum.CosPi(vec.X), TNum.CosPi(vec.Y));

    /// <inheritdoc />
    public static Vector2<TNum> Sin(Vector2<TNum> vec)
        => new(TNum.Sin(vec.X), TNum.Sin(vec.Y));

    /// <inheritdoc />
    public static (Vector2<TNum> Sin, Vector2<TNum> Cos) SinCos(Vector2<TNum> vec)
        => (Sin(vec), Cos(vec));

    /// <inheritdoc />
    public static (Vector2<TNum> SinPi, Vector2<TNum> CosPi) SinCosPi(Vector2<TNum> x)
        => (SinPi(x), CosPi(x));

    /// <inheritdoc />
    public static Vector2<TNum> SinPi(Vector2<TNum> vec)
        => new(TNum.SinPi(vec.X), TNum.SinPi(vec.Y));

    /// <inheritdoc />
    public static Vector2<TNum> Tan(Vector2<TNum> vec)
        => new(TNum.Tan(vec.X), TNum.Tan(vec.Y));

    /// <inheritdoc />
    public static Vector2<TNum> TanPi(Vector2<TNum> vec)
        => new(TNum.TanPi(vec.X), TNum.TanPi(vec.Y));

    /// <inheritdoc />
    public static bool IsNegative(Vector2<TNum> value)
        => TNum.IsNegative(value.Sum);

    /// <inheritdoc />
    public static bool IsNegativeInfinity(Vector2<TNum> value)
        => TNum.IsNegativeInfinity(value.Sum);

    /// <inheritdoc />
    public static bool IsNormal(Vector2<TNum> value)
        => value.Sum <= TNum.One;

    /// <inheritdoc />
    public static bool IsOddInteger(Vector2<TNum> value)
        => TNum.IsOddInteger(value.Sum);

    /// <inheritdoc />
    public static bool IsPositive(Vector2<TNum> value)
        => TNum.IsPositive(value.Sum);

    /// <inheritdoc />
    public static bool IsPositiveInfinity(Vector2<TNum> value)
        => TNum.IsPositiveInfinity(value.Sum);

    /// <inheritdoc />
    public static bool IsRealNumber(Vector2<TNum> value)
        => TNum.IsRealNumber(value.Sum);

    /// <inheritdoc />
    public static bool IsSubnormal(Vector2<TNum> value)
        => TNum.IsSubnormal(value.Sum);

    /// <inheritdoc />
    public static bool IsZero(Vector2<TNum> value)
        => value == Zero;

    /// <inheritdoc />
    public static Vector2<TNum> MaxMagnitude(Vector2<TNum> x, Vector2<TNum> y)
    {
        var xMagnitude = x.SquaredLength;
        var yMagnitude = y.SquaredLength;
        return xMagnitude > yMagnitude ? x : y;
    }


    /// <inheritdoc />
    public static Vector2<TNum> MaxMagnitudeNumber(Vector2<TNum> x, Vector2<TNum> y)
    {
        var xMagnitude = x.SquaredLength;
        var yMagnitude = y.SquaredLength;
        if (TNum.IsNaN(xMagnitude)) return y;
        if (TNum.IsNaN(yMagnitude)) return x;
        return xMagnitude > yMagnitude ? x : y;
    }

    /// <inheritdoc />
    public static Vector2<TNum> MinMagnitude(Vector2<TNum> x, Vector2<TNum> y)
    {
        var xMagnitude = x.SquaredLength;
        var yMagnitude = y.SquaredLength;
        if (TNum.IsNaN(xMagnitude)) return y;
        if (TNum.IsNaN(yMagnitude)) return x;
        return xMagnitude < yMagnitude ? x : y;
    }
    /// <inheritdoc />
    public static Vector2<TNum> MinMagnitudeNumber(Vector2<TNum> x, Vector2<TNum> y)
    {
        var xMagnitude = x.SquaredLength;
        var yMagnitude = y.SquaredLength;
        if (TNum.IsNaN(xMagnitude)) return y;
        if (TNum.IsNaN(yMagnitude)) return x;
        return xMagnitude < yMagnitude ? x : y;
    }

    /// <inheritdoc />
    public static int Radix => TNum.Radix;

    /// <inheritdoc />
    public static Vector2<TNum> Atan2(Vector2<TNum> y, Vector2<TNum> x)
        => new(TNum.Atan2(y.X, x.X), TNum.Atan2(y.Y, x.Y));

    /// <inheritdoc />
    public static Vector2<TNum> Atan2Pi(Vector2<TNum> y, Vector2<TNum> x)
        => new(TNum.Atan2Pi(y.X, x.X), TNum.Atan2Pi(y.Y, x.Y));


    /// <inheritdoc />
    public static Vector2<TNum> BitDecrement(Vector2<TNum> x)
        => new(TNum.BitDecrement(x.X), TNum.BitDecrement(x.Y));

    /// <inheritdoc />
    public static Vector2<TNum> BitIncrement(Vector2<TNum> x)
        => new(TNum.BitIncrement(x.X), TNum.BitIncrement(x.Y));

    /// <inheritdoc />
    public static Vector2<TNum> FusedMultiplyAdd(Vector2<TNum> l, Vector2<TNum> r, Vector2<TNum> addend)
        => new(TNum.FusedMultiplyAdd(l.X, r.X, addend.X),
            TNum.FusedMultiplyAdd(l.Y, r.Y, addend.Y));

    /// <inheritdoc />
    public static Vector2<TNum> Ieee754Remainder(Vector2<TNum> left, Vector2<TNum> right)
        => new(TNum.Ieee754Remainder(left.X, right.X),
            TNum.Ieee754Remainder(left.Y, right.Y));

    /// <inheritdoc />
    public static int ILogB(Vector2<TNum> x)
        => TNum.ILogB(x.Sum);

    /// <inheritdoc />
    public static Vector2<TNum> ScaleB(Vector2<TNum> x, int n)
        => new(TNum.ScaleB(x.X, n), TNum.ScaleB(x.Y,n));


    /// <inheritdoc />
    public static Vector2<TNum> operator *(Vector2<TNum> left, Vector2<TNum> right)
    =>new(left.X * right.X, left.Y * right.Y);

    public static Vector2<TNum> Min(Vector2<TNum> l, Vector2<TNum> r) 
        => new(TNum.Min(l.X,r.X), TNum.Min(l.Y,r.Y));
    public static Vector2<TNum> Max(Vector2<TNum> l, Vector2<TNum> r)
    =>new(TNum.Max(l.X, r.X), TNum.Max(l.Y, r.Y));

    
    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider,
        out Vector2<TNum> result)
    {
        var buf = new TNum[Dimensions];
        var success = ArrayParser.TryParse(s, style, provider, buf);
        if (!success)
        {
            result = default!;
            return false;
        }

        result = FromComponents(buf);
        return true;
    }

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider,
        out Vector2<TNum> result)
    {
        result = default;
        return s is not null && TryParse(s.AsSpan(), style, provider, out result!);
    }

    /// <inheritdoc />
    public static Vector2<TNum> Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider=null)
        => TryParse(s, style, provider, out var result) ? result : throw new FormatException();

    /// <inheritdoc />
    public static Vector2<TNum> Parse(string s, NumberStyles style, IFormatProvider? provider=null)
        => TryParse(s, style, provider, out var result) ? result : throw new FormatException();


    /// <inheritdoc />
    public static Vector2<TNum> Parse(string s, IFormatProvider? provider=null)
        => TryParse(s, NumberStyles.Any, provider, out var result) ? result : throw new FormatException();
    

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Vector2<TNum> result)
        => TryParse(s, NumberStyles.Any, provider, out result);


    /// <inheritdoc />
    public static Vector2<TNum> Parse(ReadOnlySpan<char> s, IFormatProvider? provider=null)
        => TryParse(s, NumberStyles.Any, provider, out var result) ? result : throw new FormatException();


    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Vector2<TNum> result)
        => TryParse(s, NumberStyles.Any, provider, out result);


    public override string ToString()
        => $"{this}";
    public string ToString(string? format, IFormatProvider? formatProvider=null)
    {
        formatProvider ??= CultureInfo.InvariantCulture;
        // ReSharper disable once HeapView.ObjectAllocation
        FormattableString formattable = $"{this}";
        return formattable.ToString(formatProvider);
    }
    
    
    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) 
        => ArrayParser.TryFormat(this.AsSpan(), destination, out charsWritten, format, provider);
}
