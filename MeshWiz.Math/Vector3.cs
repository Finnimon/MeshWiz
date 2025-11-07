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
public readonly struct Vector3<TNum>(TNum x, TNum y, TNum z) : IVector3<Vector3<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly TNum X = x, Y = y, Z = z;
    public TNum Sum => X + Y + Z;

    public static unsafe int ByteSize
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = sizeof(TNum) * 3;

    public int Count => 3;

    [Pure] public Vector3<TNum> Normalized => this / Length;

    /// <inheritdoc />
    public static Vector3<TNum> FromValue<TOtherNum>(TOtherNum other) where TOtherNum : INumberBase<TOtherNum>
        => new(TNum.CreateTruncating(other));

    [Pure] public static uint Dimensions => 3;
    [Pure] public TNum Length => TNum.Sqrt(SquaredLength);

    [Pure]
    public TNum SquaredLength
        => X * X + Y * Y + Z * Z;

    public TNum AlignedCuboidVolume => TNum.Abs(X * Y * Z);

    public Vector3(TNum value) : this(value, value, value) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> FromXYZ(TNum x, TNum y, TNum z)
        => new(x, y, z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> FromComponents<TList>(TList components)
        where TList : IReadOnlyList<TNum>
        => new(components[0], components[1], components[2]);


    /// <inheritdoc />
    public static Vector3<TNum> FromComponentsConstrained<TList, TOtherNum>(TList components)
        where TList : IReadOnlyList<TOtherNum> where TOtherNum : INumberBase<TOtherNum>
        => FromComponentsConstrained(components.Select(TNum.CreateTruncating).ToArray());

    /// <inheritdoc />
    public static Vector3<TNum> FromComponentsConstrained<TList>(TList components) where TList : IReadOnlyList<TNum>
        => components.Count switch
        {
            0 => Zero,
            1 => new(components[0], TNum.Zero, TNum.Zero),
            2 => new(components[0], components[1], TNum.Zero),
            _ => new(components[0], components[1], components[2])
        };


    /// <inheritdoc />
    public static Vector3<TNum> FromValue(TNum value)
        => new(value);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> FromComponents<TList, TOtherNum>(TList components)
        where TList : IReadOnlyList<TOtherNum>
        where TOtherNum : INumberBase<TOtherNum>
        => new(TNum.CreateTruncating(components[0]),
            TNum.CreateTruncating(components[1]),
            TNum.CreateTruncating(components[2]));


    public Vector3<TNum> Inverse => Invert(this);


    public Vector3<TNum> ZYX => new(Z, Y, Y);
    public Vector3<TNum> YZX => new(Y, Z, Y);
    public Vector3<TNum> YXZ => new(Y, Y, Z);
    public Vector3<TNum> XZY => new(Y, Z, Y);
    public Vector3<TNum> ZXY => new(Z, Y, Y);
    public Vector3<TNum> XXX => new(X);
    public Vector3<TNum> YYY => new(Y);
    public Vector3<TNum> ZZZ => new(Z);

    public static Vector3<TNum> Zero
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = new(TNum.Zero, TNum.Zero, TNum.Zero);


    public static Vector3<TNum> One
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = new(TNum.One, TNum.One, TNum.One);

    public static Vector3<TNum> NaN
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = new(TNum.NaN, TNum.NaN, TNum.NaN);

    /// <inheritdoc />
    public static Vector3<TNum> NegativeInfinity { get; } = new(TNum.NegativeInfinity);

    /// <inheritdoc />
    public static Vector3<TNum> NegativeZero { get; } = new(TNum.NegativeZero);

    /// <inheritdoc />
    public static Vector3<TNum> PositiveInfinity { get; } = new(TNum.PositiveInfinity);

    public static Vector3<TNum> UnitX
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = new(TNum.One, TNum.Zero, TNum.Zero);

    public static Vector3<TNum> UnitY
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = new(TNum.Zero, TNum.One, TNum.Zero);

    public static Vector3<TNum> UnitZ
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = new(TNum.Zero, TNum.Zero, TNum.One);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<TOther> To<TOther>() where TOther : unmanaged, IFloatingPointIeee754<TOther>
        => new(TOther.CreateTruncating(X), TOther.CreateTruncating(Y), TOther.CreateTruncating(Z));

    public static implicit operator Vector3(Vector3<TNum> v)
        => new(float.CreateTruncating(v.X), float.CreateTruncating(v.Y), float.CreateTruncating(v.Z));

    public static implicit operator Vector3<TNum>(Vector3 v)
        => new(TNum.CreateTruncating(v.X), TNum.CreateTruncating(v.Y), TNum.CreateTruncating(v.Z));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator +(Vector3<TNum> left, Vector3<TNum> right)
        => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator -(Vector3<TNum> left, Vector3<TNum> right)
        => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    // [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static TNum operator *(Vector3<TNum> left, Vector3<TNum> right)
    //     => left.X * right.X + left.Y * right.Y + left.Z * right.Z;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator *(Vector3<TNum> vec, TNum scalar)
        => new(x: vec.X * scalar, y: vec.Y * scalar, z: vec.Z * scalar);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator *(TNum scalar, Vector3<TNum> vec)
        => new(vec.X * scalar, vec.Y * scalar, vec.Z * scalar);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator -(Vector3<TNum> vec) => new(-vec.X, -vec.Y, -vec.Z);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator /(Vector3<TNum> vec, TNum divisor)
        => vec * (TNum.One / divisor);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator /(TNum num, Vector3<TNum> vec)
        => new(num / vec.X, num / vec.Y, num / vec.Z);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator ^(Vector3<TNum> left, Vector3<TNum> right)
        => new(
            x: left.Y * right.Z - left.Z * right.Y,
            y: left.Z * right.X - left.X * right.Z,
            z: left.X * right.Y - left.Y * right.X
        );

    [Pure, SuppressMessage("ReSharper", "CompareOfTNumsByEqualityOperator"),
     MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vector3<TNum> left, Vector3<TNum> right)
        => left.X == right.X && left.Y == right.Y && left.Z == right.Z;

    [Pure, SuppressMessage("ReSharper", "CompareOfTNumsByEqualityOperator"),
     MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vector3<TNum> left, Vector3<TNum> right)
        => left.X != right.X || left.Y != right.Y || left.Z != right.Z;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<TNum> Add(Vector3<TNum> other)
        => new(X + other.X, Y + other.Y, Z + other.Z);


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<TNum> Subtract(Vector3<TNum> other)
        => this - other;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<TNum> Scale(TNum scalar)
        => this * scalar;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<TNum> Divide(TNum divisor)
        => this / divisor;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum Dot(Vector3<TNum> other)
        => X * other.X + Y * other.Y + Z * other.Z;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum DistanceTo(Vector3<TNum> other) => (this - other).Length;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum SquaredDistanceTo(Vector3<TNum> other) => (this - other).SquaredLength;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<TNum> Cross(Vector3<TNum> other) => this ^ other;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsParallelTo(Vector3<TNum> other, TNum tolerance)
        => tolerance >= TNum.Abs(TNum.Abs(Normalized.Dot(other.Normalized)) - TNum.One);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsParallelTo(Vector3<TNum> other)
        => IsParallelTo(other, TNum.Epsilon);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Vector3<TNum> other)
        => this == other;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(Vector3<TNum> other)
        => SquaredLength.CompareTo(other.SquaredLength);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ReadOnlySpan<TNum> AsSpan()
    {
        fixed (TNum* ptr = &X) return new ReadOnlySpan<TNum>(ptr, 3);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? other)
        => other is Vector3<TNum> vec && vec == this;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    [Pure]
    public unsafe TNum this[int index]
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (Dimensions <= (uint)index) IndexThrowHelper.Throw(index, Count);
            fixed (TNum* ptr = &X)
                return ptr[index];
        }
    }

    public TNum this[CoordinateAxis axis]
        => this[(int)axis];

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

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out TNum x, out TNum y, out TNum z)
    {
        x = X;
        y = Y;
        z = Z;
    }


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> Lerp(Vector3<TNum> a, Vector3<TNum> b, TNum t)
    {
        var negT = TNum.One - t;
        return new(
            x: b.X * t + a.X * negT,
            y: b.Y * t + a.Y * negT,
            z: b.Z * t + a.Z * negT
        );
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> Lerp(Vector3<TNum> a, Vector3<TNum> b, Vector3<TNum> t)
        => new(x: b.X * t.X + a.X * (TNum.One - t.X),
            y: b.Y * t.Y + a.Y * (TNum.One - t.Y),
            z: b.Z * t.Z + a.Z * (TNum.One - t.Z)
        );


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> ExactLerp(Vector3<TNum> from, Vector3<TNum> toward, TNum exactDistance)
    {
        var len = toward.DistanceTo(from);
        var lerpFactor = exactDistance / len;
        return Lerp(from, toward, lerpFactor);
    }


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> CosineLerp(Vector3<TNum> from, Vector3<TNum> to, TNum normalDistance)
    {
        var two = TNum.CreateTruncating(2);
        normalDistance = normalDistance.Wrap(TNum.Zero, two);
        var sineDistance = TNum.Sin(normalDistance * TNum.Pi / two);
        sineDistance = TNum.Clamp(sineDistance, TNum.Zero, TNum.One);
        return Lerp(from, to, sineDistance);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> Invert(Vector3<TNum> vec)
        => TNum.One / vec;

    public static Vector3<TNum> Pow(Vector3<TNum> v, TNum power)
        => new(TNum.Pow(v.X, power),
            TNum.Pow(v.Y, power),
            TNum.Pow(v.Z, power));

    public static Vector3<TNum> Squared(Vector3<TNum> v)
        => new(v.X * v.X, v.Y * v.Y, v.Z * v.Z);

    public static Vector3<TNum> ElementWiseMul(Vector3<TNum> l, Vector3<TNum> r)
        => new(l.X * r.X, l.Y * r.Y, l.Z * r.Z);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> Min(Vector3<TNum> left, Vector3<TNum> right)
        => new(TNum.Min(left.X, right.X), TNum.Min(left.Y, right.Y), TNum.Min(left.Z, right.Z));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> Max(Vector3<TNum> left, Vector3<TNum> right)
        => new(TNum.Max(left.X, right.X), TNum.Max(left.Y, right.Y), TNum.Max(left.Z, right.Z));

    /// <inheritdoc />
    public int CompareTo(object? obj) => obj is Vector3<TNum> vec ? CompareTo(vec) : 1;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage("ReSharper", "EqualExpressionComparison")]
    // [SuppressMessage("ReSharper", "EqualExpressionComparison")]
    public static bool IsNaN(Vector3<TNum> vec)
#pragma warning disable CS1718
        => vec != vec;
#pragma warning restore CS1718

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ray3<TNum> RayThrough(Vector3<TNum> through) => new Ray3<TNum>(this, through - this);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Line<Vector3<TNum>, TNum> LineTo(Vector3<TNum> end) => new(this, end);


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsApprox(Vector3<TNum> other, TNum squareTolerance) => SquaredDistanceTo(other) < squareTolerance;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsApprox(Vector3<TNum> other) => (this - other).IsApproxZero();


    /// <inheritdoc />
    public static Vector3<TNum> operator %(Vector3<TNum> l, Vector3<TNum> r)
        => new(l.X % r.X, l.Y % r.Y, l.Z % r.Z);

    /// <inheritdoc />
    public static Vector3<TNum> operator +(Vector3<TNum> v)
        => new(+v.X, +v.Y, +v.Z);

    /// <inheritdoc />
    public static Vector3<TNum> operator *(Vector3<TNum> left, Vector3<TNum> right)
        => new(left.X * right.X, left.Y * right.Y, left.Z * right.Z);

    /// <inheritdoc />
    public static Vector3<TNum> Pow(Vector3<TNum> x, Vector3<TNum> y)
        => new(TNum.Pow(x.X, y.X), TNum.Pow(x.Y, y.Y), TNum.Pow(x.Z, y.Z));

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> Abs(Vector3<TNum> v)
        => new(TNum.Abs(v.X), TNum.Abs(v.Y), TNum.Abs(v.Z));

    /// <inheritdoc />
    public static bool IsCanonical(Vector3<TNum> value)
        => TNum.IsCanonical(value.Sum);

    /// <inheritdoc />
    public static bool IsComplexNumber(Vector3<TNum> v)
        => TNum.IsComplexNumber(v.Sum);

    /// <inheritdoc />
    public static bool IsEvenInteger(Vector3<TNum> v)
        => TNum.IsEvenInteger(v.Sum);

    /// <inheritdoc />
    public static bool IsFinite(Vector3<TNum> v)
        => TNum.IsFinite(v.Sum);

    /// <inheritdoc />
    public static bool IsImaginaryNumber(Vector3<TNum> value)
        => TNum.IsImaginaryNumber(value.Sum);

    /// <inheritdoc />
    public static bool IsInfinity(Vector3<TNum> value)
        => TNum.IsInfinity(value.Sum);

    /// <inheritdoc />
    public static bool IsInteger(Vector3<TNum> value)
        => TNum.IsInteger(value.Sum);

    /// <inheritdoc />
    public static Vector3<TNum> AdditiveIdentity => Zero;


    /// <inheritdoc />
    public static bool operator >(Vector3<TNum> left, Vector3<TNum> right)
    {
        var max = Max(left, right);
        return max == left && max != right;
    }

    /// <inheritdoc />
    public static bool operator >=(Vector3<TNum> left, Vector3<TNum> right)
        => Max(left, right) == left;

    /// <inheritdoc />
    public static bool operator <(Vector3<TNum> left, Vector3<TNum> right)
        => right > left;

    /// <inheritdoc />
    public static bool operator <=(Vector3<TNum> left, Vector3<TNum> right)
        => right >= left;

    /// <inheritdoc />
    public static Vector3<TNum> operator --(Vector3<TNum> v)
        => v - One;

    /// <inheritdoc />
    public static Vector3<TNum> operator /(Vector3<TNum> l, Vector3<TNum> r)
        => new(l.X / r.X, l.Y / r.Y, l.Z / r.Z);

    /// <inheritdoc />
    public static Vector3<TNum> operator ++(Vector3<TNum> v)
        => v + One;

    /// <inheritdoc />
    public static Vector3<TNum> MultiplicativeIdentity => One;

    /// <inheritdoc />
    public static Vector3<TNum> E { get; } = new(TNum.E);

    /// <inheritdoc />
    public static Vector3<TNum> Pi { get; } = new(TNum.Pi);

    /// <inheritdoc />
    public static Vector3<TNum> Tau { get; } = new(TNum.Tau);

    /// <inheritdoc />
    public static Vector3<TNum> Exp(Vector3<TNum> v)
        => new(TNum.Exp(v.X), TNum.Exp(v.Y), TNum.Exp(v.Z));

    /// <inheritdoc />
    public static Vector3<TNum> Exp10(Vector3<TNum> v)
        => new(TNum.Exp10(v.X), TNum.Exp10(v.Y), TNum.Exp10(v.Z));

    /// <inheritdoc />
    public static Vector3<TNum> Exp2(Vector3<TNum> v)
        => new(TNum.Exp2(v.X), TNum.Exp2(v.Y), TNum.Exp2(v.Z));


    /// <inheritdoc />
    public static Vector3<TNum> NegativeOne { get; } = new(TNum.NegativeOne);


    /// <inheritdoc />
    public static Vector3<TNum> Round(Vector3<TNum> vec, int digits, MidpointRounding mode)
        => new(TNum.Round(vec.X, digits, mode),
            TNum.Round(vec.Y, digits, mode),
            TNum.Round(vec.Z, digits, mode));


    /// <inheritdoc />
    public static Vector3<TNum> Acosh(Vector3<TNum> vec)
        => new(TNum.Acosh(vec.X), TNum.Acosh(vec.Y), TNum.Acosh(vec.Z));

    /// <inheritdoc />
    public static Vector3<TNum> Asinh(Vector3<TNum> vec)
        => new(TNum.Asinh(vec.X), TNum.Asinh(vec.Y), TNum.Asinh(vec.Z));

    /// <inheritdoc />
    public static Vector3<TNum> Atanh(Vector3<TNum> vec)
        => new(TNum.Atanh(vec.X), TNum.Atanh(vec.Y), TNum.Atanh(vec.Z));

    /// <inheritdoc />
    public static Vector3<TNum> Cosh(Vector3<TNum> vec)
        => new(TNum.Cosh(vec.X), TNum.Cosh(vec.Y), TNum.Cosh(vec.Z));


    /// <inheritdoc />
    public static Vector3<TNum> Sinh(Vector3<TNum> vec)
        => new(TNum.Sinh(vec.X), TNum.Sinh(vec.Y), TNum.Sinh(vec.Z));


    /// <inheritdoc />
    public static Vector3<TNum> Tanh(Vector3<TNum> vec)
        => new(TNum.Tanh(vec.X), TNum.Tanh(vec.Y), TNum.Tanh(vec.Z));

    /// <inheritdoc />
    public static Vector3<TNum> Log(Vector3<TNum> vec)
        => new(TNum.Log(vec.X), TNum.Log(vec.Y), TNum.Log(vec.Z));

    /// <inheritdoc />
    public static Vector3<TNum> Log(Vector3<TNum> vec, Vector3<TNum> newBase)
        => new(TNum.Log(vec.X, newBase.X),
            TNum.Log(vec.Y, newBase.Y),
            TNum.Log(vec.Z, newBase.Z));

    /// <inheritdoc />
    public static Vector3<TNum> Log10(Vector3<TNum> vec)
        => new(TNum.Log10(vec.X), TNum.Log10(vec.Y), TNum.Log10(vec.Z));

    /// <inheritdoc />
    public static Vector3<TNum> Log2(Vector3<TNum> vec)
        => new(TNum.Log2(vec.X), TNum.Log2(vec.Y), TNum.Log2(vec.Z));


    /// <inheritdoc />
    public static Vector3<TNum> Cbrt(Vector3<TNum> vec)
        => new(TNum.Cbrt(vec.X), TNum.Cbrt(vec.Y), TNum.Cbrt(vec.Z));

    /// <inheritdoc />
    public static Vector3<TNum> Hypot(Vector3<TNum> x, Vector3<TNum> y)
        => new(TNum.Hypot(x.X, y.X), TNum.Hypot(x.Y, y.Y), TNum.Hypot(x.Z, y.Z));


    /// <inheritdoc />
    public static Vector3<TNum> RootN(Vector3<TNum> vec, int n)
        => new(TNum.RootN(vec.X, n), TNum.RootN(vec.Y, n), TNum.RootN(vec.Z, n));


    /// <inheritdoc />
    public static Vector3<TNum> Sqrt(Vector3<TNum> vec)
        => new(TNum.Sqrt(vec.X), TNum.Sqrt(vec.Y), TNum.Sqrt(vec.Z));


    /// <inheritdoc />
    public static Vector3<TNum> Acos(Vector3<TNum> vec)
        => new(TNum.Acos(vec.X), TNum.Acos(vec.Y), TNum.Acos(vec.Z));

    /// <inheritdoc />
    public static Vector3<TNum> AcosPi(Vector3<TNum> vec)
        => new(TNum.AcosPi(vec.X), TNum.AcosPi(vec.Y), TNum.AcosPi(vec.Z));


    /// <inheritdoc />
    public static Vector3<TNum> Asin(Vector3<TNum> vec)
        => new(TNum.Asin(vec.X), TNum.Asin(vec.Y), TNum.Asin(vec.Z));


    /// <inheritdoc />
    public static Vector3<TNum> AsinPi(Vector3<TNum> vec)
        => new(TNum.AsinPi(vec.X), TNum.AsinPi(vec.Y), TNum.AsinPi(vec.Z));


    /// <inheritdoc />
    public static Vector3<TNum> Atan(Vector3<TNum> vec)
        => new(TNum.Atan(vec.X), TNum.Atan(vec.Y), TNum.Atan(vec.Z));


    /// <inheritdoc />
    public static Vector3<TNum> AtanPi(Vector3<TNum> vec)
        => new(TNum.AtanPi(vec.X), TNum.AtanPi(vec.Y), TNum.AtanPi(vec.Z));


    /// <inheritdoc />
    public static Vector3<TNum> Cos(Vector3<TNum> vec)
        => new(TNum.Cos(vec.X), TNum.Cos(vec.Y), TNum.Cos(vec.Z));

    /// <inheritdoc />
    public static Vector3<TNum> CosPi(Vector3<TNum> vec)
        => new(TNum.CosPi(vec.X), TNum.CosPi(vec.Y), TNum.CosPi(vec.Z));

    /// <inheritdoc />
    public static Vector3<TNum> Sin(Vector3<TNum> vec)
        => new(TNum.Sin(vec.X), TNum.Sin(vec.Y), TNum.Sin(vec.Z));

    /// <inheritdoc />
    public static (Vector3<TNum> Sin, Vector3<TNum> Cos) SinCos(Vector3<TNum> vec)
        => (Sin(vec), Cos(vec));

    /// <inheritdoc />
    public static (Vector3<TNum> SinPi, Vector3<TNum> CosPi) SinCosPi(Vector3<TNum> x)
        => (SinPi(x), CosPi(x));

    /// <inheritdoc />
    public static Vector3<TNum> SinPi(Vector3<TNum> vec)
        => new(TNum.SinPi(vec.X), TNum.SinPi(vec.Y), TNum.SinPi(vec.Z));

    /// <inheritdoc />
    public static Vector3<TNum> Tan(Vector3<TNum> vec)
        => new(TNum.Tan(vec.X), TNum.Tan(vec.Y), TNum.Tan(vec.Z));

    /// <inheritdoc />
    public static Vector3<TNum> TanPi(Vector3<TNum> vec)
        => new(TNum.TanPi(vec.X), TNum.TanPi(vec.Y), TNum.TanPi(vec.Z));

    /// <inheritdoc />
    public static bool IsNegative(Vector3<TNum> value)
        => TNum.IsNegative(value.Sum);

    /// <inheritdoc />
    public static bool IsNegativeInfinity(Vector3<TNum> value)
        => TNum.IsNegativeInfinity(value.Sum);

    /// <inheritdoc />
    public static bool IsNormal(Vector3<TNum> value)
        => value.SquaredLength <= TNum.One;

    public bool IsNormalized => this.SquaredLength.IsApprox(TNum.One, Numbers<TNum>.ZeroEpsilon);

    /// <inheritdoc />
    public static bool IsOddInteger(Vector3<TNum> value)
        => TNum.IsOddInteger(value.Sum);

    /// <inheritdoc />
    public static bool IsPositive(Vector3<TNum> value)
        => TNum.IsPositive(value.Sum);

    /// <inheritdoc />
    public static bool IsPositiveInfinity(Vector3<TNum> value)
        => TNum.IsPositiveInfinity(value.Sum);

    /// <inheritdoc />
    public static bool IsRealNumber(Vector3<TNum> value)
        => TNum.IsRealNumber(value.Sum);

    /// <inheritdoc />
    public static bool IsSubnormal(Vector3<TNum> value)
        => TNum.IsSubnormal(value.Sum);

    /// <inheritdoc />
    public static bool IsZero(Vector3<TNum> value)
        => value == Zero;

    /// <inheritdoc />
    public static Vector3<TNum> MaxMagnitude(Vector3<TNum> x, Vector3<TNum> y)
    {
        var xMagnitude = x.SquaredLength;
        var yMagnitude = y.SquaredLength;
        return xMagnitude > yMagnitude ? x : y;
    }


    /// <inheritdoc />
    public static Vector3<TNum> MaxMagnitudeNumber(Vector3<TNum> x, Vector3<TNum> y)
    {
        var xMagnitude = x.SquaredLength;
        var yMagnitude = y.SquaredLength;
        if (TNum.IsNaN(xMagnitude)) return y;
        if (TNum.IsNaN(yMagnitude)) return x;
        return xMagnitude > yMagnitude ? x : y;
    }

    /// <inheritdoc />
    public static Vector3<TNum> MinMagnitude(Vector3<TNum> x, Vector3<TNum> y)
    {
        var xMagnitude = x.SquaredLength;
        var yMagnitude = y.SquaredLength;
        if (TNum.IsNaN(xMagnitude)) return y;
        if (TNum.IsNaN(yMagnitude)) return x;
        return xMagnitude < yMagnitude ? x : y;
    }

    /// <inheritdoc />
    public static Vector3<TNum> MinMagnitudeNumber(Vector3<TNum> x, Vector3<TNum> y)
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
    public static Vector3<TNum> Atan2(Vector3<TNum> y, Vector3<TNum> x)
        => new(TNum.Atan2(y.X, x.X), TNum.Atan2(y.Y, x.Y), TNum.Atan2(y.Z, x.Z));

    /// <inheritdoc />
    public static Vector3<TNum> Atan2Pi(Vector3<TNum> y, Vector3<TNum> x)
        => new(TNum.Atan2Pi(y.X, x.X), TNum.Atan2Pi(y.Y, x.Y), TNum.Atan2Pi(y.Z, x.Z));


    /// <inheritdoc />
    public static Vector3<TNum> BitDecrement(Vector3<TNum> x)
        => new(TNum.BitDecrement(x.X), TNum.BitDecrement(x.Y), TNum.BitDecrement(x.Z));

    /// <inheritdoc />
    public static Vector3<TNum> BitIncrement(Vector3<TNum> x)
        => new(TNum.BitIncrement(x.X), TNum.BitIncrement(x.Y), TNum.BitIncrement(x.Z));

    /// <inheritdoc />
    public static Vector3<TNum> FusedMultiplyAdd(Vector3<TNum> l, Vector3<TNum> r, Vector3<TNum> addend)
        => new(TNum.FusedMultiplyAdd(l.X, r.X, addend.X),
            TNum.FusedMultiplyAdd(l.Y, r.Y, addend.Y),
            TNum.FusedMultiplyAdd(l.Z, r.Z, addend.Z));

    /// <inheritdoc />
    public static Vector3<TNum> Ieee754Remainder(Vector3<TNum> left, Vector3<TNum> right)
        => new(TNum.Ieee754Remainder(left.X, right.X),
            TNum.Ieee754Remainder(left.Y, right.Y),
            TNum.Ieee754Remainder(left.Z, right.Z));

    /// <inheritdoc />
    public static int ILogB(Vector3<TNum> x)
        => TNum.ILogB(x.Sum);

    /// <inheritdoc />
    public static Vector3<TNum> ScaleB(Vector3<TNum> x, int n)
        => new(TNum.ScaleB(x.X, n), TNum.ScaleB(x.Y, n), TNum.ScaleB(x.Z, n));

    /// <inheritdoc />
    public static Vector3<TNum> Epsilon { get; } = new(TNum.Epsilon);


    /// <inheritdoc />
    public static Vector3<TNum> Parse(string s, IFormatProvider? provider = null)
        => Parse(s.AsSpan(), NumberStyles.Any, provider);

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Vector3<TNum> result)
        => TryParse(s, NumberStyles.Any, provider, out result);

    /// <inheritdoc />
    public static Vector3<TNum> Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
        => TryParse(s, provider, out var result) ? result : throw new FormatException();

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Vector3<TNum> result)
        => TryParse(s, NumberStyles.Any, provider, out result);

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider,
        out Vector3<TNum> result)
    {
        var components = new TNum[Dimensions];
        var success = ArrayParser.TryParse(s, style, provider, components);
        result = FromComponents(components);
        return success;
    }

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider,
        out Vector3<TNum> result)
    {
        result = default;
        return s is not null && TryParse(s.AsSpan(), style, provider, out result);
    }


    /// <inheritdoc />
    public static Vector3<TNum> Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider = null)
        => TryParse(s, style, provider, out var result) ? result : throw new FormatException();


    /// <inheritdoc />
    public static Vector3<TNum> Parse(string s, NumberStyles style, IFormatProvider? provider = null)
        => Parse(s.AsSpan(), style, provider);


    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format,
        IFormatProvider? provider = null)
        => ArrayParser.TryFormat(AsSpan(), destination, out charsWritten, format, provider);


    public override string ToString()
        => ToString("G", CultureInfo.CurrentCulture);

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        Span<char> buffer = stackalloc char[64]; // plenty for two numbers
        if (TryFormat(buffer, out int charsWritten, format, formatProvider))
            return new string(buffer[..charsWritten]);
        buffer = stackalloc char[128];
        if (TryFormat(buffer, out charsWritten, format, formatProvider))
            return new string(buffer[..charsWritten]);
        throw new InvalidOperationException();
    }

    [Pure]
    public TNum AngleTo(Vector3<TNum> other) => AngleBetween(this, other);

    [Pure]
    public static TNum AngleBetween(Vector3<TNum> a, Vector3<TNum> b)
    {
        a = a.Normalized;
        b = b.Normalized;
        var dot = a.Dot(b);
        return TNum.Acos(dot);
    }

    [Pure]
    public static TNum SignedAngleBetween(Vector3<TNum> a, Vector3<TNum> b, Vector3<TNum> about)
    {
        var plane = new Plane3<TNum>(about, TNum.Zero);
        var a2D = plane.ProjectIntoLocal(a).Normalized;
        var b2D = plane.ProjectIntoLocal(b).Normalized;

        var dp = a2D.Dot(b2D);
        dp = TNum.Clamp(dp, TNum.NegativeOne, TNum.One);
        var angle = TNum.Acos(dp);

        var z = a2D.Cross(b2D);
        return TNum.CopySign(angle, z);
    }


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> FusedMultiplyAdd(Vector3<TNum> a, TNum b, Vector3<TNum> addend)
        => new(TNum.FusedMultiplyAdd(a.X, b, addend.X), TNum.FusedMultiplyAdd(a.Y, b, addend.Y),
            TNum.FusedMultiplyAdd(a.Z, b, addend.Z));
}