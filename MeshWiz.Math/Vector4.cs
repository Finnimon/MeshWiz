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
public readonly struct Vector4<TNum> : IFloatingVector<Vector4<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly TNum X, Y, Z, W;

    public static unsafe int ByteSize
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = sizeof(TNum) * 4;

    public int Count => 4;
    public TNum Sum => X + Y + Z + W;

    /// <inheritdoc />
    public static Vector4<TNum> FromValue<TOtherNum>(TOtherNum other) where TOtherNum : INumberBase<TOtherNum>
        => new(TNum.CreateTruncating(other));

    static uint IVector<Vector4<TNum>, TNum>.Dimensions => 4;
    public Vector4<TNum> Normalized => this / Length;

    [Pure] public static uint Dimensions => 4;
    [Pure] public TNum Length => TNum.Sqrt(SquaredLength);

    [Pure]
    public TNum SquaredLength
        => X * X + Y * Y + Z * Z + W * W;

    public unsafe Vector3<TNum> XYZ
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            fixed (void* ptr = &this) return *(Vector3<TNum>*)ptr;
        }
    }


    public Vector4(TNum x, TNum y, TNum z, TNum w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector4(Vector3<TNum> xyz, TNum w) => ((X, Y, Z), W) = (xyz, w);

    public Vector4(Vector3<TNum> vec3)
    {
        X = vec3.X;
        Y = vec3.Y;
        Z = vec3.Z;
        W = TNum.Zero;
    }

    public Vector4(TNum value) : this(value, value, value, value) { }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> FromXYZW(TNum x, TNum y, TNum z, TNum w)
        => new(x, y, z, w);

    public Vector4<TOtherNum> To<TOtherNum>()
        where TOtherNum : unmanaged, IFloatingPointIeee754<TOtherNum>
        => new(
            TOtherNum.CreateTruncating(X),
            TOtherNum.CreateTruncating(Y),
            TOtherNum.CreateTruncating(Z),
            TOtherNum.CreateTruncating(W)
        );

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> FromComponents<TList>(TList components)
        where TList : IReadOnlyList<TNum>
        => new(components[0], components[1], components[2], components[3]);


    /// <inheritdoc />
    public static Vector4<TNum> FromComponentsConstrained<TList, TOtherNum>(TList components)
        where TList : IReadOnlyList<TOtherNum> where TOtherNum : INumberBase<TOtherNum>
        => FromComponentsConstrained(components.Select(TNum.CreateTruncating).ToArray());

    /// <inheritdoc />
    public static Vector4<TNum> FromComponentsConstrained<TList>(TList components) where TList : IReadOnlyList<TNum>
        => components.Count switch
        {
            0 => Zero,
            1 => new(components[0], TNum.Zero, TNum.Zero, TNum.Zero),
            2 => new(components[0], components[1], TNum.Zero, TNum.Zero),
            3 => new(components[0], components[1], components[2], TNum.Zero),
            _ => new(components[0], components[1], components[2], components[3])
        };

    /// <inheritdoc />
    public static Vector4<TNum> FromValue(TNum value)
        => new(value);


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> FromComponents<TList, TOtherNum>(TList components)
        where TList : IReadOnlyList<TOtherNum>
        where TOtherNum : INumberBase<TOtherNum>
        => new(TNum.CreateTruncating(components[0]),
            TNum.CreateTruncating(components[1]),
            TNum.CreateTruncating(components[2]),
            TNum.CreateTruncating(components[3]));


    public static Vector4<TNum> Zero
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = new(TNum.Zero, TNum.Zero, TNum.Zero, TNum.Zero);


    public static Vector4<TNum> One
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = new(TNum.One, TNum.One, TNum.One, TNum.One);

    /// <inheritdoc />
    public static Vector4<TNum> Epsilon { get; } = new(TNum.Epsilon);

    public static Vector4<TNum> NaN
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = new(TNum.NaN, TNum.NaN, TNum.NaN, TNum.NaN);

    /// <inheritdoc />
    public static Vector4<TNum> NegativeInfinity { get; } = new(TNum.NegativeInfinity);

    /// <inheritdoc />
    public static Vector4<TNum> NegativeZero { get; } = new(TNum.NegativeZero);

    /// <inheritdoc />
    public static Vector4<TNum> PositiveInfinity { get; } = new(TNum.PositiveInfinity);

    public static Vector4<TNum> UnitX
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = new(TNum.One, TNum.Zero, TNum.Zero, TNum.Zero);

    public static Vector4<TNum> UnitY
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = new(TNum.Zero, TNum.One, TNum.Zero, TNum.Zero);

    public static Vector4<TNum> UnitZ
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = new(TNum.Zero, TNum.Zero, TNum.One, TNum.Zero);

    public static Vector4<TNum> UnitW
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = new(TNum.Zero, TNum.Zero, TNum.Zero, TNum.One);


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> operator +(Vector4<TNum> left, Vector4<TNum> right)
        => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> operator -(Vector4<TNum> left, Vector4<TNum> right)
        => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> operator -(Vector4<TNum> vec) => new(-vec.X, -vec.Y, -vec.Z, -vec.W);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> operator *(Vector4<TNum> left, Vector4<TNum> right)
        => new(left.X * right.X, left.Y * right.Y, left.Z * right.Z, right.W * right.W);


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> operator *(Vector4<TNum> vec, TNum scalar)
        => new(x: vec.X * scalar, y: vec.Y * scalar, z: vec.Z * scalar, vec.W * scalar);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> operator *(TNum scalar, Vector4<TNum> vec)
        => new(vec.X * scalar, vec.Y * scalar, vec.Z * scalar, vec.W * scalar);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> operator /(Vector4<TNum> vec, TNum divisor)
        => vec * (TNum.One / divisor);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> operator /(TNum dividend, Vector4<TNum> divisor)
        => new Vector4<TNum>(dividend) / divisor;


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
    public TNum Dot(Vector4<TNum> other)
        => X * other.X + Y * other.Y + Z * other.Z + other.W * other.W;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum DistanceTo(Vector4<TNum> other) => (this - other).Length;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum SquaredDistanceTo(Vector4<TNum> other) => (this - other).SquaredLength;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsParallelTo(Vector4<TNum> other, TNum tolerance)
        => tolerance >= TNum.Abs(Normalized.Dot(other.Normalized)) - TNum.One;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsParallelTo(Vector4<TNum> other)
        => IsParallelTo(other, TNum.Epsilon);

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
    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);

    [Pure]
    public unsafe TNum this[int index]
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (Dimensions > (uint)index)
                fixed (TNum* ptr = &X)
                    return ptr[index];
            throw new IndexOutOfRangeException();
        }
    }

    [Pure]
    internal unsafe TNum this[uint index]
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            fixed (TNum* ptr = &X) return ptr[index];
        }
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    public void Deconstruct(out TNum x, out TNum y, out TNum z, out TNum w)
    {
        x = X;
        y = Y;
        z = Z;
        w = W;
    }

    #endregion

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> Lerp(Vector4<TNum> from, Vector4<TNum> to, TNum t)
        => (to - from) * t + from;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> ExactLerp(Vector4<TNum> from, Vector4<TNum> toward, TNum exactDistance)
        => (toward - from).Normalized * exactDistance + from;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> CosineLerp(Vector4<TNum> from, Vector4<TNum> to, TNum normalDistance)
    {
        var two = TNum.CreateTruncating(2);
        normalDistance = normalDistance.Wrap(TNum.Zero, two);
        var cosDistance = (-TNum.Cos(normalDistance * TNum.Pi) + TNum.One) / two;
        return Lerp(from, to, cosDistance);
    }


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage("ReSharper", "EqualExpressionComparison")]
    // [SuppressMessage("ReSharper", "EqualExpressionComparison")]
    public static bool IsNaN(Vector4<TNum> vec)
#pragma warning disable CS1718
        => vec != vec;
#pragma warning restore CS1718

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsApprox(Vector4<TNum> other, TNum squareTolerance) => SquaredDistanceTo(other) < squareTolerance;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsApprox(Vector4<TNum> other) => SquaredDistanceTo(other) <= TNum.Epsilon;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Line<Vector4<TNum>, TNum> LineTo(Vector4<TNum> end) => new(this, end);

    public static implicit operator Vector4<TNum>(Vector3<TNum> xyz)
        => new(xyz);


    /// <inheritdoc />
    public static Vector4<TNum> operator %(Vector4<TNum> l, Vector4<TNum> r)
        => new(l.X % r.X, l.Y % r.Y, l.Z % r.Z, l.W % r.W);

    /// <inheritdoc />
    public static Vector4<TNum> operator +(Vector4<TNum> v)
        => new(+v.X, +v.Y, +v.Z, +v.W);

    /// <inheritdoc />
    public static Vector4<TNum> Pow(Vector4<TNum> x, Vector4<TNum> y)
        => new(TNum.Pow(x.X, y.X), TNum.Pow(x.Y, y.Y), TNum.Pow(x.Z, y.Z), TNum.Pow(x.W, y.W));

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4<TNum> Abs(Vector4<TNum> v)
        => new(TNum.Abs(v.X), TNum.Abs(v.Y), TNum.Abs(v.Z), TNum.Abs(v.W));

    /// <inheritdoc />
    public static bool IsCanonical(Vector4<TNum> value)
        => TNum.IsCanonical(value.Sum);

    /// <inheritdoc />
    public static bool IsComplexNumber(Vector4<TNum> v)
        => TNum.IsComplexNumber(v.Sum);

    /// <inheritdoc />
    public static bool IsEvenInteger(Vector4<TNum> v)
        => TNum.IsEvenInteger(v.Sum);

    /// <inheritdoc />
    public static bool IsFinite(Vector4<TNum> v)
        => TNum.IsFinite(v.Sum);

    /// <inheritdoc />
    public static bool IsImaginaryNumber(Vector4<TNum> value)
        => TNum.IsImaginaryNumber(value.Sum);

    /// <inheritdoc />
    public static bool IsInfinity(Vector4<TNum> value)
        => TNum.IsInfinity(value.Sum);

    /// <inheritdoc />
    public static bool IsInteger(Vector4<TNum> value)
        => TNum.IsInteger(value.Sum);

    /// <inheritdoc />
    public static Vector4<TNum> AdditiveIdentity => Zero;

    
    /// <inheritdoc />
    public static bool operator >(Vector4<TNum> left, Vector4<TNum> right)
    {
        var max= Max(left, right);
        return max == left && max != right;
    }

    /// <inheritdoc />
    public static bool operator >=(Vector4<TNum> left, Vector4<TNum> right)
        => Max(left,right)==left;

    /// <inheritdoc />
    public static bool operator <(Vector4<TNum> left, Vector4<TNum> right)
        => right>left;

    /// <inheritdoc />
    public static bool operator <=(Vector4<TNum> left, Vector4<TNum> right)
        => right >= left;
    
    /// <inheritdoc />
    public static Vector4<TNum> operator --(Vector4<TNum> v)
        => v - One;

    /// <inheritdoc />
    public static Vector4<TNum> operator /(Vector4<TNum> l, Vector4<TNum> r)
        => new(l.X / r.X, l.Y / r.Y, l.Z / r.Z, l.W / r.W);

    /// <inheritdoc />
    public static Vector4<TNum> operator ++(Vector4<TNum> v)
        => v + One;

    /// <inheritdoc />
    public static Vector4<TNum> MultiplicativeIdentity => One;

    /// <inheritdoc />
    public static Vector4<TNum> E { get; } = new(TNum.E);

    /// <inheritdoc />
    public static Vector4<TNum> Pi { get; } = new(TNum.Pi);

    /// <inheritdoc />
    public static Vector4<TNum> Tau { get; } = new(TNum.Tau);

    /// <inheritdoc />
    public static Vector4<TNum> Exp(Vector4<TNum> v)
        => new(TNum.Exp(v.X), TNum.Exp(v.Y), TNum.Exp(v.Z), TNum.Exp(v.W));

    /// <inheritdoc />
    public static Vector4<TNum> Exp10(Vector4<TNum> v)
        => new(TNum.Exp10(v.X), TNum.Exp10(v.Y), TNum.Exp10(v.Z), TNum.Exp10(v.W));

    /// <inheritdoc />
    public static Vector4<TNum> Exp2(Vector4<TNum> v)
        => new(TNum.Exp2(v.X), TNum.Exp2(v.Y), TNum.Exp2(v.Z), TNum.Exp2(v.W));


    /// <inheritdoc />
    public static Vector4<TNum> NegativeOne { get; } = new(TNum.NegativeOne);


    /// <inheritdoc />
    public static Vector4<TNum> Round(Vector4<TNum> vec, int digits, MidpointRounding mode)
        => new(TNum.Round(vec.X, digits, mode),
            TNum.Round(vec.Y, digits, mode),
            TNum.Round(vec.Z, digits, mode),
            TNum.Round(vec.W, digits, mode));


    /// <inheritdoc />
    public static Vector4<TNum> Acosh(Vector4<TNum> vec)
        => new(TNum.Acosh(vec.X), TNum.Acosh(vec.Y), TNum.Acosh(vec.Z), TNum.Acosh(vec.W));

    /// <inheritdoc />
    public static Vector4<TNum> Asinh(Vector4<TNum> vec)
        => new(TNum.Asinh(vec.X), TNum.Asinh(vec.Y), TNum.Asinh(vec.Z), TNum.Asinh(vec.W));

    /// <inheritdoc />
    public static Vector4<TNum> Atanh(Vector4<TNum> vec)
        => new(TNum.Atanh(vec.X), TNum.Atanh(vec.Y), TNum.Atanh(vec.Z), TNum.Atanh(vec.W));

    /// <inheritdoc />
    public static Vector4<TNum> Cosh(Vector4<TNum> vec)
        => new(TNum.Cosh(vec.X), TNum.Cosh(vec.Y), TNum.Cosh(vec.Z), TNum.Cosh(vec.W));


    /// <inheritdoc />
    public static Vector4<TNum> Sinh(Vector4<TNum> vec)
        => new(TNum.Sinh(vec.X), TNum.Sinh(vec.Y), TNum.Sinh(vec.Z), TNum.Sinh(vec.W));


    /// <inheritdoc />
    public static Vector4<TNum> Tanh(Vector4<TNum> vec)
        => new(TNum.Tanh(vec.X), TNum.Tanh(vec.Y), TNum.Tanh(vec.Z), TNum.Tanh(vec.W));

    /// <inheritdoc />
    public static Vector4<TNum> Log(Vector4<TNum> vec)
        => new(TNum.Log(vec.X), TNum.Log(vec.Y), TNum.Log(vec.Z), TNum.Log(vec.W));

    /// <inheritdoc />
    public static Vector4<TNum> Log(Vector4<TNum> vec, Vector4<TNum> newBase)
        => new(TNum.Log(vec.X, newBase.X),
            TNum.Log(vec.Y, newBase.Y),
            TNum.Log(vec.Z, newBase.Z),
            TNum.Log(vec.W, newBase.W));

    /// <inheritdoc />
    public static Vector4<TNum> Log10(Vector4<TNum> vec)
        => new(TNum.Log10(vec.X), TNum.Log10(vec.Y), TNum.Log10(vec.Z), TNum.Log10(vec.W));

    /// <inheritdoc />
    public static Vector4<TNum> Log2(Vector4<TNum> vec)
        => new(TNum.Log2(vec.X), TNum.Log2(vec.Y), TNum.Log2(vec.Z), TNum.Log2(vec.W));


    /// <inheritdoc />
    public static Vector4<TNum> Cbrt(Vector4<TNum> vec)
        => new(TNum.Cbrt(vec.X), TNum.Cbrt(vec.Y), TNum.Cbrt(vec.Z), TNum.Cbrt(vec.W));

    /// <inheritdoc />
    public static Vector4<TNum> Hypot(Vector4<TNum> x, Vector4<TNum> y)
        => new(TNum.Hypot(x.X, y.X), TNum.Hypot(x.Y, y.Y), TNum.Hypot(x.Z, y.Z), TNum.Hypot(x.W, y.W));


    /// <inheritdoc />
    public static Vector4<TNum> RootN(Vector4<TNum> vec, int n)
        => new(TNum.RootN(vec.X, n), TNum.RootN(vec.Y, n), TNum.RootN(vec.Z, n),
            TNum.RootN(vec.W, n));


    /// <inheritdoc />
    public static Vector4<TNum> Sqrt(Vector4<TNum> vec)
        => new(TNum.Sqrt(vec.X), TNum.Sqrt(vec.Y), TNum.Sqrt(vec.Z), TNum.Sqrt(vec.W));


    /// <inheritdoc />
    public static Vector4<TNum> Acos(Vector4<TNum> vec)
        => new(TNum.Acos(vec.X), TNum.Acos(vec.Y), TNum.Acos(vec.Z), TNum.Acos(vec.W));

    /// <inheritdoc />
    public static Vector4<TNum> AcosPi(Vector4<TNum> vec)
        => new(TNum.AcosPi(vec.X), TNum.AcosPi(vec.Y), TNum.AcosPi(vec.Z), TNum.AcosPi(vec.W));


    /// <inheritdoc />
    public static Vector4<TNum> Asin(Vector4<TNum> vec)
        => new(TNum.Asin(vec.X), TNum.Asin(vec.Y), TNum.Asin(vec.Z), TNum.Asin(vec.W));


    /// <inheritdoc />
    public static Vector4<TNum> AsinPi(Vector4<TNum> vec)
        => new(TNum.AsinPi(vec.X), TNum.AsinPi(vec.Y), TNum.AsinPi(vec.Z), TNum.AsinPi(vec.W));


    /// <inheritdoc />
    public static Vector4<TNum> Atan(Vector4<TNum> vec)
        => new(TNum.Atan(vec.X), TNum.Atan(vec.Y), TNum.Atan(vec.Z), TNum.Atan(vec.W));


    /// <inheritdoc />
    public static Vector4<TNum> AtanPi(Vector4<TNum> vec)
        => new(TNum.AtanPi(vec.X), TNum.AtanPi(vec.Y), TNum.AtanPi(vec.Z), TNum.AtanPi(vec.W));


    /// <inheritdoc />
    public static Vector4<TNum> Cos(Vector4<TNum> vec)
        => new(TNum.Cos(vec.X), TNum.Cos(vec.Y), TNum.Cos(vec.Z), TNum.Cos(vec.W));

    /// <inheritdoc />
    public static Vector4<TNum> CosPi(Vector4<TNum> vec)
        => new(TNum.CosPi(vec.X), TNum.CosPi(vec.Y), TNum.CosPi(vec.Z), TNum.CosPi(vec.W));

    /// <inheritdoc />
    public static Vector4<TNum> Sin(Vector4<TNum> vec)
        => new(TNum.Sin(vec.X), TNum.Sin(vec.Y), TNum.Sin(vec.Z), TNum.Sin(vec.W));

    /// <inheritdoc />
    public static (Vector4<TNum> Sin, Vector4<TNum> Cos) SinCos(Vector4<TNum> vec)
        => (Sin(vec), Cos(vec));

    /// <inheritdoc />
    public static (Vector4<TNum> SinPi, Vector4<TNum> CosPi) SinCosPi(Vector4<TNum> x)
        => (SinPi(x), CosPi(x));

    /// <inheritdoc />
    public static Vector4<TNum> SinPi(Vector4<TNum> vec)
        => new(TNum.SinPi(vec.X), TNum.SinPi(vec.Y), TNum.SinPi(vec.Z), TNum.SinPi(vec.W));

    /// <inheritdoc />
    public static Vector4<TNum> Tan(Vector4<TNum> vec)
        => new(TNum.Tan(vec.X), TNum.Tan(vec.Y), TNum.Tan(vec.Z), TNum.Tan(vec.W));

    /// <inheritdoc />
    public static Vector4<TNum> TanPi(Vector4<TNum> vec)
        => new(TNum.TanPi(vec.X), TNum.TanPi(vec.Y), TNum.TanPi(vec.Z), TNum.TanPi(vec.W));

    /// <inheritdoc />
    public static bool IsNegative(Vector4<TNum> value)
        => TNum.IsNegative(value.Sum);

    /// <inheritdoc />
    public static bool IsNegativeInfinity(Vector4<TNum> value)
        => TNum.IsNegativeInfinity(value.Sum);

    /// <inheritdoc />
    public static bool IsNormal(Vector4<TNum> value)
        => value.Sum <= TNum.One;

    /// <inheritdoc />
    public static bool IsOddInteger(Vector4<TNum> value)
        => TNum.IsOddInteger(value.Sum);

    /// <inheritdoc />
    public static bool IsPositive(Vector4<TNum> value)
        => TNum.IsPositive(value.Sum);

    /// <inheritdoc />
    public static bool IsPositiveInfinity(Vector4<TNum> value)
        => TNum.IsPositiveInfinity(value.Sum);

    /// <inheritdoc />
    public static bool IsRealNumber(Vector4<TNum> value)
        => TNum.IsRealNumber(value.Sum);

    /// <inheritdoc />
    public static bool IsSubnormal(Vector4<TNum> value)
        => TNum.IsSubnormal(value.Sum);

    /// <inheritdoc />
    public static bool IsZero(Vector4<TNum> value)
        => value == Zero;


    /// <inheritdoc />
    public static int Radix => TNum.Radix;

    /// <inheritdoc />
    public static Vector4<TNum> Atan2(Vector4<TNum> y, Vector4<TNum> x)
        => new(TNum.Atan2(y.X, x.X), TNum.Atan2(y.Y, x.Y), TNum.Atan2(y.Z, x.Z), TNum.Atan2(y.W, x.W));

    /// <inheritdoc />
    public static Vector4<TNum> Atan2Pi(Vector4<TNum> y, Vector4<TNum> x)
        => new(TNum.Atan2Pi(y.X, x.X), TNum.Atan2Pi(y.Y, x.Y), TNum.Atan2Pi(y.Z, x.Z), TNum.Atan2Pi(y.W, x.W));


    /// <inheritdoc />
    public static Vector4<TNum> BitDecrement(Vector4<TNum> x)
        => new(TNum.BitDecrement(x.X), TNum.BitDecrement(x.Y), TNum.BitDecrement(x.Z), TNum.BitDecrement(x.W));

    /// <inheritdoc />
    public static Vector4<TNum> BitIncrement(Vector4<TNum> x)
        => new(TNum.BitIncrement(x.X), TNum.BitIncrement(x.Y), TNum.BitIncrement(x.Z), TNum.BitIncrement(x.W));

    /// <inheritdoc />
    public static Vector4<TNum> FusedMultiplyAdd(Vector4<TNum> l, Vector4<TNum> r, Vector4<TNum> addend)
        => new(TNum.FusedMultiplyAdd(l.X, r.X, addend.X),
            TNum.FusedMultiplyAdd(l.Y, r.Y, addend.Y),
            TNum.FusedMultiplyAdd(l.Z, r.Z, addend.Z),
            TNum.FusedMultiplyAdd(l.W, r.W, addend.W));

    /// <inheritdoc />
    public static Vector4<TNum> Ieee754Remainder(Vector4<TNum> left, Vector4<TNum> right)
        => new(TNum.Ieee754Remainder(left.X, right.X),
            TNum.Ieee754Remainder(left.Y, right.Y),
            TNum.Ieee754Remainder(left.Z, right.Z),
            TNum.Ieee754Remainder(left.W, right.W));

    /// <inheritdoc />
    public static int ILogB(Vector4<TNum> x)
        => TNum.ILogB(x.Sum);

    /// <inheritdoc />
    public static Vector4<TNum> ScaleB(Vector4<TNum> x, int n)
        => new(TNum.ScaleB(x.X, n), TNum.ScaleB(x.Y, n), TNum.ScaleB(x.Z, n), TNum.ScaleB(x.W, n));


    public static Vector4<TNum> Min(Vector4<TNum> l, Vector4<TNum> r)
        => new(TNum.Min(l.X, r.X), TNum.Min(l.Y, r.Y), TNum.Min(l.Z, r.Z), TNum.Min(l.W, r.W));

    public static Vector4<TNum> Max(Vector4<TNum> l, Vector4<TNum> r)
        => new(TNum.Max(l.X, r.X), TNum.Max(l.Y, r.Y), TNum.Max(l.Z, r.Z), TNum.Max(l.W, r.W));

    public static Vector4<TNum> Clamp(Vector4<TNum> vec, Vector4<TNum> min, Vector4<TNum> max)
        => new(TNum.Clamp(vec.X, min.X, max.X),
            TNum.Clamp(vec.Y, min.Y, max.Y),
            TNum.Clamp(vec.Z, min.Z, max.Z),
            TNum.Clamp(vec.W, min.W, max.W));


    /// <inheritdoc />
    public static Vector4<TNum> MaxMagnitude(Vector4<TNum> x, Vector4<TNum> y)
    {
        var xMagnitude = x.SquaredLength;
        var yMagnitude = y.SquaredLength;
        return xMagnitude > yMagnitude ? x : y;
    }


    /// <inheritdoc />
    public static Vector4<TNum> MaxMagnitudeNumber(Vector4<TNum> x, Vector4<TNum> y)
    {
        var xMagnitude = x.SquaredLength;
        var yMagnitude = y.SquaredLength;
        if (TNum.IsNaN(xMagnitude)) return y;
        if (TNum.IsNaN(yMagnitude)) return x;
        return xMagnitude > yMagnitude ? x : y;
    }

    /// <inheritdoc />
    public static Vector4<TNum> MinMagnitude(Vector4<TNum> x, Vector4<TNum> y)
    {
        var xMagnitude = x.SquaredLength;
        var yMagnitude = y.SquaredLength;
        if (TNum.IsNaN(xMagnitude)) return y;
        if (TNum.IsNaN(yMagnitude)) return x;
        return xMagnitude < yMagnitude ? x : y;
    }

    /// <inheritdoc />
    public static Vector4<TNum> MinMagnitudeNumber(Vector4<TNum> x, Vector4<TNum> y)
    {
        var xMagnitude = x.SquaredLength;
        var yMagnitude = y.SquaredLength;
        if (TNum.IsNaN(xMagnitude)) return y;
        if (TNum.IsNaN(yMagnitude)) return x;
        return xMagnitude < yMagnitude ? x : y;
    }

    /// <inheritdoc />
    public int CompareTo(object? obj)
        => obj is not Vector4<TNum> v ? 1 : CompareTo(v);


    /// <inheritdoc />
    public static Vector4<TNum> Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider = null)
        => TryParse(s, style, provider, out var result) ? result : throw new FormatException();

    /// <inheritdoc />
    public static Vector4<TNum> Parse(string s, NumberStyles style, IFormatProvider? provider = null)
        => TryParse(s, style, provider, out var result) ? result : throw new FormatException();

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider,
        out Vector4<TNum> result)
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
        out Vector4<TNum> result)
    {
        result = default;
        return s is not null && TryParse(s.AsSpan(), style, provider, out result!);
    }

    /// <inheritdoc />
    public static Vector4<TNum> Parse(string s, IFormatProvider? provider = null)
        => TryParse(s, NumberStyles.Any, provider, out var result) ? result : throw new FormatException();

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Vector4<TNum> result)
        => TryParse(s, NumberStyles.Any, provider, out result);

    /// <inheritdoc />
    public static Vector4<TNum> Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
        => TryParse(s, NumberStyles.Any, provider, out var result) ? result : throw new FormatException();

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Vector4<TNum> result)
        => TryParse(s, NumberStyles.Any, provider, out result);

    
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


    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format,
        IFormatProvider? provider = null)
        => ArrayParser.TryFormat(this.AsSpan(), destination, out charsWritten, format, provider);
}