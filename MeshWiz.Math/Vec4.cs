using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Vec4<TNum> : IVec<Vec4<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly TNum X, Y, Z, W;

    public static int ByteSize { get; } = Unsafe.SizeOf<TNum>() * 4;

    public int Count => 4;
    public TNum Sum => X + Y + Z + W;

    /// <inheritdoc />
    public static Vec4<TNum> FromValue<TOtherNum>(TOtherNum other) where TOtherNum : INumberBase<TOtherNum>
        => new(TNum.CreateTruncating(other));

    public Vec4<TNum> Normalized() => this / Length;

    [Pure] public static int Dimensions => 4;
    [Pure] public TNum Length => TNum.Sqrt(SquaredLength);

    [Pure]
    public TNum SquaredLength
        => X * X + Y * Y + Z * Z + W * W;

    public Vec3<TNum> XYZ => Unsafe.As<Vec4<TNum>, Vec3<TNum>>(ref Unsafe.AsRef(in this));


    public Vec4(TNum x, TNum y, TNum z, TNum w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec4(Vec3<TNum> xyz, TNum w) => ((X, Y, Z), W) = (xyz, w);

    public Vec4(Vec3<TNum> vec3)
    {
        X = vec3.X;
        Y = vec3.Y;
        Z = vec3.Z;
        W = TNum.Zero;
    }

    public Vec4(TNum value) : this(value, value, value, value) { }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec4<TNum> Create(TNum x, TNum y, TNum z, TNum w)
        => new(x, y, z, w);

    public Vec4<TOtherNum> To<TOtherNum>()
        where TOtherNum : unmanaged, IFloatingPointIeee754<TOtherNum>
        => new(
            TOtherNum.CreateTruncating(X),
            TOtherNum.CreateTruncating(Y),
            TOtherNum.CreateTruncating(Z),
            TOtherNum.CreateTruncating(W)
        );

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec4<TNum> FromComponents<TList>(TList components)
        where TList : IReadOnlyList<TNum>
        => new(components[0], components[1], components[2], components[3]);


    /// <inheritdoc />
    public static Vec4<TNum> FromComponentsConstrained<TList, TOtherNum>(TList components)
        where TList : IReadOnlyList<TOtherNum> where TOtherNum : INumberBase<TOtherNum>
        => FromComponentsConstrained(components.Select(TNum.CreateTruncating).ToArray());

    /// <inheritdoc />
    public static Vec4<TNum> FromComponentsConstrained<TList>(TList components) where TList : IReadOnlyList<TNum>
        => components.Count switch
        {
            0 => Zero,
            1 => new Vec4<TNum>(components[0], TNum.Zero, TNum.Zero, TNum.Zero),
            2 => new Vec4<TNum>(components[0], components[1], TNum.Zero, TNum.Zero),
            3 => new Vec4<TNum>(components[0], components[1], components[2], TNum.Zero),
            _ => new Vec4<TNum>(components[0], components[1], components[2], components[3])
        };

    /// <inheritdoc />
    public static Vec4<TNum> FromValue(TNum value)
        => new(value);


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec4<TNum> FromComponents<TList, TOtherNum>(TList components)
        where TList : IReadOnlyList<TOtherNum>
        where TOtherNum : INumberBase<TOtherNum>
        => new(TNum.CreateTruncating(components[0]),
            TNum.CreateTruncating(components[1]),
            TNum.CreateTruncating(components[2]),
            TNum.CreateTruncating(components[3]));


    public static Vec4<TNum> Zero => new(TNum.Zero, TNum.Zero, TNum.Zero, TNum.Zero);


    public static Vec4<TNum> One => new(TNum.One);

    /// <inheritdoc />
    public static Vec4<TNum> Epsilon => new(TNum.Epsilon);

    public static Vec4<TNum> NaN => new(TNum.NaN);

    /// <inheritdoc />
    public static Vec4<TNum> NegativeInfinity => new(TNum.NegativeInfinity);

    /// <inheritdoc />
    public static Vec4<TNum> NegativeZero => new(TNum.NegativeZero);

    /// <inheritdoc />
    public static Vec4<TNum> PositiveInfinity => new(TNum.PositiveInfinity);

    public static Vec4<TNum> UnitX => new(TNum.One, TNum.Zero, TNum.Zero, TNum.Zero);

    public static Vec4<TNum> UnitY => new(TNum.Zero, TNum.One, TNum.Zero, TNum.Zero);

    public static Vec4<TNum> UnitZ => new(TNum.Zero, TNum.Zero, TNum.One, TNum.Zero);

    public static Vec4<TNum> UnitW => new(TNum.Zero, TNum.Zero, TNum.Zero, TNum.One);


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec4<TNum> operator +(Vec4<TNum> left, Vec4<TNum> right)
        => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec4<TNum> operator -(Vec4<TNum> left, Vec4<TNum> right)
        => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec4<TNum> operator -(Vec4<TNum> vec) => new(-vec.X, -vec.Y, -vec.Z, -vec.W);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec4<TNum> operator *(Vec4<TNum> left, Vec4<TNum> right)
        => new(left.X * right.X, left.Y * right.Y, left.Z * right.Z, right.W * right.W);


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec4<TNum> operator *(Vec4<TNum> vec, TNum scalar)
        => new(x: vec.X * scalar, y: vec.Y * scalar, z: vec.Z * scalar, vec.W * scalar);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec4<TNum> operator *(TNum scalar, Vec4<TNum> vec)
        => new(vec.X * scalar, vec.Y * scalar, vec.Z * scalar, vec.W * scalar);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec4<TNum> operator /(Vec4<TNum> vec, TNum divisor)
        => vec * (TNum.One / divisor);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec4<TNum> operator /(TNum dividend, Vec4<TNum> divisor)
        => new Vec4<TNum>(dividend) / divisor;


    [Pure, SuppressMessage("ReSharper", "CompareOfTNumsByEqualityOperator")]
    public static bool operator ==(Vec4<TNum> left, Vec4<TNum> right)
        => left.X == right.X && left.Y == right.Y && left.Z == right.Z;

    [Pure, SuppressMessage("ReSharper", "CompareOfTNumsByEqualityOperator")]
    public static bool operator !=(Vec4<TNum> left, Vec4<TNum> right)
        => left.X != right.X || left.Y != right.Y || left.Z != right.Z;


    #region functions

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec4<TNum> Add(Vec4<TNum> other)
        => this + other;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec4<TNum> Subtract(Vec4<TNum> other)
        => this - other;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec4<TNum> Scale(TNum scalar)
        => this * scalar;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec4<TNum> Divide(TNum divisor)
        => this / divisor;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum Dot(Vec4<TNum> other)
        => Dot(this,other);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TNum Dot(Vec4<TNum> a, Vec4<TNum> b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum DistanceTo(Vec4<TNum> other) => Distance(this, other);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum SquaredDistanceTo(Vec4<TNum> other) => SquaredDistance(this, other);

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TNum Distance(Vec4<TNum> a, Vec4<TNum> b) => TNum.Sqrt(SquaredDistance(a, b));

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TNum SquaredDistance(Vec4<TNum> a, Vec4<TNum> b)
    {
        var x = a.X - b.X;
        var y = a.Y - b.Y;
        var z = a.Z - b.Z;
        var w = a.W - b.W;
        return x * x + y * y + z * z + w * w;
    }


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsParallelTo(Vec4<TNum> other, TNum tolerance)
        => tolerance >= TNum.Abs(Normalized().Dot(other.Normalized())) - TNum.One;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsParallelTo(Vec4<TNum> other)
        => IsParallelTo(other, TNum.Epsilon);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Vec4<TNum> other)
        => this == other;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(Vec4<TNum> other)
        => SquaredLength.CompareTo(other.SquaredLength);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ReadOnlySpan<TNum> AsSpan()
    {
        fixed (TNum* ptr = &X) return new ReadOnlySpan<TNum>(ptr, 4);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? other)
        => other is Vec4<TNum> vec && vec == this;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);

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
    public static Vec4<TNum> Lerp(Vec4<TNum> from, Vec4<TNum> to, TNum t)
        => (to - from) * t + from;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec4<TNum> ExactLerp(Vec4<TNum> from, Vec4<TNum> toward, TNum exactDistance)
    {
        var dist = Distance(from, toward);
        return Lerp(from, toward, exactDistance / dist);
    }


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec4<TNum> CosineLerp(Vec4<TNum> from, Vec4<TNum> to, TNum normalDistance)
    {
        var two = TNum.CreateTruncating(2);
        normalDistance = normalDistance.Wrap(TNum.Zero, two);
        var cosDistance = (-TNum.Cos(normalDistance * TNum.Pi) + TNum.One) / two;
        return Lerp(from, to, cosDistance);
    }


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage("ReSharper", "EqualExpressionComparison")]
    // [SuppressMessage("ReSharper", "EqualExpressionComparison")]
    public static bool IsNaN(Vec4<TNum> vec)
#pragma warning disable CS1718
        => vec != vec;
#pragma warning restore CS1718

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsApprox(Vec4<TNum> other, TNum squareTolerance) => SquaredDistanceTo(other) < squareTolerance;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsApprox(Vec4<TNum> other) => SquaredDistanceTo(other) <= TNum.Epsilon;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Line<Vec4<TNum>, TNum> LineTo(Vec4<TNum> end) => new(this, end);

    public static implicit operator Vec4<TNum>(Vec3<TNum> xyz)
        => new(xyz);

    public static implicit operator Vec4<TNum>(Vec4<float> v) => v.To<TNum>();
    public static implicit operator Vec4<TNum>(Vec4<double> v) => v.To<TNum>();
    public static implicit operator Vec4<TNum>(Vec4<Half> v) => v.To<TNum>();
    public static implicit operator Vec4<float>(Vec4<TNum> v) => v.To<float>();
    public static implicit operator Vec4<double>(Vec4<TNum> v) => v.To<double>();
    public static implicit operator Vec4<Half>(Vec4<TNum> v) => v.To<Half>();

    /// <inheritdoc />
    public static Vec4<TNum> operator %(Vec4<TNum> l, Vec4<TNum> r)
        => new(l.X % r.X, l.Y % r.Y, l.Z % r.Z, l.W % r.W);

    /// <inheritdoc />
    public static Vec4<TNum> operator +(Vec4<TNum> v)
        => new(+v.X, +v.Y, +v.Z, +v.W);

    /// <inheritdoc />
    public static Vec4<TNum> Pow(Vec4<TNum> x, Vec4<TNum> y)
        => new(TNum.Pow(x.X, y.X), TNum.Pow(x.Y, y.Y), TNum.Pow(x.Z, y.Z), TNum.Pow(x.W, y.W));

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec4<TNum> Abs(Vec4<TNum> v)
        => new(TNum.Abs(v.X), TNum.Abs(v.Y), TNum.Abs(v.Z), TNum.Abs(v.W));

    /// <inheritdoc />
    public static bool IsCanonical(Vec4<TNum> value)
        => TNum.IsCanonical(value.Sum);

    /// <inheritdoc />
    public static bool IsComplexNumber(Vec4<TNum> v)
        => TNum.IsComplexNumber(v.Sum);

    /// <inheritdoc />
    public static bool IsEvenInteger(Vec4<TNum> v)
        => TNum.IsEvenInteger(v.Sum);

    /// <inheritdoc />
    public static bool IsFinite(Vec4<TNum> v)
        => TNum.IsFinite(v.Sum);

    /// <inheritdoc />
    public static bool IsImaginaryNumber(Vec4<TNum> value)
        => TNum.IsImaginaryNumber(value.Sum);

    /// <inheritdoc />
    public static bool IsInfinity(Vec4<TNum> value)
        => TNum.IsInfinity(value.Sum);

    /// <inheritdoc />
    public static bool IsInteger(Vec4<TNum> value)
        => TNum.IsInteger(value.Sum);

    /// <inheritdoc />
    public static Vec4<TNum> AdditiveIdentity => Zero;


    /// <inheritdoc />
    public static bool operator >(Vec4<TNum> left, Vec4<TNum> right)
    {
        var max = Max(left, right);
        return max == left && max != right;
    }

    /// <inheritdoc />
    public static bool operator >=(Vec4<TNum> left, Vec4<TNum> right)
        => Max(left, right) == left;

    /// <inheritdoc />
    public static bool operator <(Vec4<TNum> left, Vec4<TNum> right)
        => right > left;

    /// <inheritdoc />
    public static bool operator <=(Vec4<TNum> left, Vec4<TNum> right)
        => right >= left;

    /// <inheritdoc />
    public static Vec4<TNum> operator --(Vec4<TNum> v)
        => v - One;

    /// <inheritdoc />
    public static Vec4<TNum> operator /(Vec4<TNum> l, Vec4<TNum> r)
        => new(l.X / r.X, l.Y / r.Y, l.Z / r.Z, l.W / r.W);

    /// <inheritdoc />
    public static Vec4<TNum> operator ++(Vec4<TNum> v)
        => v + One;

    /// <inheritdoc />
    public static Vec4<TNum> MultiplicativeIdentity => One;

    /// <inheritdoc />
    public static Vec4<TNum> E => new(TNum.E);

    /// <inheritdoc />
    public static Vec4<TNum> Pi => new(TNum.Pi);

    /// <inheritdoc />
    public static Vec4<TNum> Tau => new(TNum.Tau);

    /// <inheritdoc />
    public static Vec4<TNum> Exp(Vec4<TNum> v)
        => new(TNum.Exp(v.X), TNum.Exp(v.Y), TNum.Exp(v.Z), TNum.Exp(v.W));

    /// <inheritdoc />
    public static Vec4<TNum> Exp10(Vec4<TNum> v)
        => new(TNum.Exp10(v.X), TNum.Exp10(v.Y), TNum.Exp10(v.Z), TNum.Exp10(v.W));

    /// <inheritdoc />
    public static Vec4<TNum> Exp2(Vec4<TNum> v)
        => new(TNum.Exp2(v.X), TNum.Exp2(v.Y), TNum.Exp2(v.Z), TNum.Exp2(v.W));


    /// <inheritdoc />
    public static Vec4<TNum> NegativeOne => new(TNum.NegativeOne);


    /// <inheritdoc />
    public static Vec4<TNum> Round(Vec4<TNum> vec, int digits, MidpointRounding mode)
        => new(TNum.Round(vec.X, digits, mode),
            TNum.Round(vec.Y, digits, mode),
            TNum.Round(vec.Z, digits, mode),
            TNum.Round(vec.W, digits, mode));


    /// <inheritdoc />
    public static Vec4<TNum> Acosh(Vec4<TNum> vec)
        => new(TNum.Acosh(vec.X), TNum.Acosh(vec.Y), TNum.Acosh(vec.Z), TNum.Acosh(vec.W));

    /// <inheritdoc />
    public static Vec4<TNum> Asinh(Vec4<TNum> vec)
        => new(TNum.Asinh(vec.X), TNum.Asinh(vec.Y), TNum.Asinh(vec.Z), TNum.Asinh(vec.W));

    /// <inheritdoc />
    public static Vec4<TNum> Atanh(Vec4<TNum> vec)
        => new(TNum.Atanh(vec.X), TNum.Atanh(vec.Y), TNum.Atanh(vec.Z), TNum.Atanh(vec.W));

    /// <inheritdoc />
    public static Vec4<TNum> Cosh(Vec4<TNum> vec)
        => new(TNum.Cosh(vec.X), TNum.Cosh(vec.Y), TNum.Cosh(vec.Z), TNum.Cosh(vec.W));


    /// <inheritdoc />
    public static Vec4<TNum> Sinh(Vec4<TNum> vec)
        => new(TNum.Sinh(vec.X), TNum.Sinh(vec.Y), TNum.Sinh(vec.Z), TNum.Sinh(vec.W));


    /// <inheritdoc />
    public static Vec4<TNum> Tanh(Vec4<TNum> vec)
        => new(TNum.Tanh(vec.X), TNum.Tanh(vec.Y), TNum.Tanh(vec.Z), TNum.Tanh(vec.W));

    /// <inheritdoc />
    public static Vec4<TNum> Log(Vec4<TNum> vec)
        => new(TNum.Log(vec.X), TNum.Log(vec.Y), TNum.Log(vec.Z), TNum.Log(vec.W));

    /// <inheritdoc />
    public static Vec4<TNum> Log(Vec4<TNum> vec, Vec4<TNum> newBase)
        => new(TNum.Log(vec.X, newBase.X),
            TNum.Log(vec.Y, newBase.Y),
            TNum.Log(vec.Z, newBase.Z),
            TNum.Log(vec.W, newBase.W));

    /// <inheritdoc />
    public static Vec4<TNum> Log10(Vec4<TNum> vec)
        => new(TNum.Log10(vec.X), TNum.Log10(vec.Y), TNum.Log10(vec.Z), TNum.Log10(vec.W));

    /// <inheritdoc />
    public static Vec4<TNum> Log2(Vec4<TNum> vec)
        => new(TNum.Log2(vec.X), TNum.Log2(vec.Y), TNum.Log2(vec.Z), TNum.Log2(vec.W));


    /// <inheritdoc />
    public static Vec4<TNum> Cbrt(Vec4<TNum> vec)
        => new(TNum.Cbrt(vec.X), TNum.Cbrt(vec.Y), TNum.Cbrt(vec.Z), TNum.Cbrt(vec.W));

    /// <inheritdoc />
    public static Vec4<TNum> Hypot(Vec4<TNum> x, Vec4<TNum> y)
        => new(TNum.Hypot(x.X, y.X), TNum.Hypot(x.Y, y.Y), TNum.Hypot(x.Z, y.Z), TNum.Hypot(x.W, y.W));


    /// <inheritdoc />
    public static Vec4<TNum> RootN(Vec4<TNum> vec, int n)
        => new(TNum.RootN(vec.X, n), TNum.RootN(vec.Y, n), TNum.RootN(vec.Z, n),
            TNum.RootN(vec.W, n));


    /// <inheritdoc />
    public static Vec4<TNum> Sqrt(Vec4<TNum> vec)
        => new(TNum.Sqrt(vec.X), TNum.Sqrt(vec.Y), TNum.Sqrt(vec.Z), TNum.Sqrt(vec.W));


    /// <inheritdoc />
    public static Vec4<TNum> Acos(Vec4<TNum> vec)
        => new(TNum.Acos(vec.X), TNum.Acos(vec.Y), TNum.Acos(vec.Z), TNum.Acos(vec.W));

    /// <inheritdoc />
    public static Vec4<TNum> AcosPi(Vec4<TNum> vec)
        => new(TNum.AcosPi(vec.X), TNum.AcosPi(vec.Y), TNum.AcosPi(vec.Z), TNum.AcosPi(vec.W));


    /// <inheritdoc />
    public static Vec4<TNum> Asin(Vec4<TNum> vec)
        => new(TNum.Asin(vec.X), TNum.Asin(vec.Y), TNum.Asin(vec.Z), TNum.Asin(vec.W));


    /// <inheritdoc />
    public static Vec4<TNum> AsinPi(Vec4<TNum> vec)
        => new(TNum.AsinPi(vec.X), TNum.AsinPi(vec.Y), TNum.AsinPi(vec.Z), TNum.AsinPi(vec.W));


    /// <inheritdoc />
    public static Vec4<TNum> Atan(Vec4<TNum> vec)
        => new(TNum.Atan(vec.X), TNum.Atan(vec.Y), TNum.Atan(vec.Z), TNum.Atan(vec.W));


    /// <inheritdoc />
    public static Vec4<TNum> AtanPi(Vec4<TNum> vec)
        => new(TNum.AtanPi(vec.X), TNum.AtanPi(vec.Y), TNum.AtanPi(vec.Z), TNum.AtanPi(vec.W));


    /// <inheritdoc />
    public static Vec4<TNum> Cos(Vec4<TNum> vec)
        => new(TNum.Cos(vec.X), TNum.Cos(vec.Y), TNum.Cos(vec.Z), TNum.Cos(vec.W));

    /// <inheritdoc />
    public static Vec4<TNum> CosPi(Vec4<TNum> vec)
        => new(TNum.CosPi(vec.X), TNum.CosPi(vec.Y), TNum.CosPi(vec.Z), TNum.CosPi(vec.W));

    /// <inheritdoc />
    public static Vec4<TNum> Sin(Vec4<TNum> vec)
        => new(TNum.Sin(vec.X), TNum.Sin(vec.Y), TNum.Sin(vec.Z), TNum.Sin(vec.W));

    /// <inheritdoc />
    public static (Vec4<TNum> Sin, Vec4<TNum> Cos) SinCos(Vec4<TNum> vec)
        => (Sin(vec), Cos(vec));

    /// <inheritdoc />
    public static (Vec4<TNum> SinPi, Vec4<TNum> CosPi) SinCosPi(Vec4<TNum> x)
        => (SinPi(x), CosPi(x));

    /// <inheritdoc />
    public static Vec4<TNum> SinPi(Vec4<TNum> vec)
        => new(TNum.SinPi(vec.X), TNum.SinPi(vec.Y), TNum.SinPi(vec.Z), TNum.SinPi(vec.W));

    /// <inheritdoc />
    public static Vec4<TNum> Tan(Vec4<TNum> vec)
        => new(TNum.Tan(vec.X), TNum.Tan(vec.Y), TNum.Tan(vec.Z), TNum.Tan(vec.W));

    /// <inheritdoc />
    public static Vec4<TNum> TanPi(Vec4<TNum> vec)
        => new(TNum.TanPi(vec.X), TNum.TanPi(vec.Y), TNum.TanPi(vec.Z), TNum.TanPi(vec.W));

    /// <inheritdoc />
    public static bool IsNegative(Vec4<TNum> value)
        => TNum.IsNegative(value.Sum);

    /// <inheritdoc />
    public static bool IsNegativeInfinity(Vec4<TNum> value)
        => TNum.IsNegativeInfinity(value.Sum);

    /// <inheritdoc />
    public static bool IsNormal(Vec4<TNum> value)
        => value.Sum <= TNum.One;

    /// <inheritdoc />
    public static bool IsOddInteger(Vec4<TNum> value)
        => TNum.IsOddInteger(value.Sum);

    /// <inheritdoc />
    public static bool IsPositive(Vec4<TNum> value)
        => TNum.IsPositive(value.Sum);

    /// <inheritdoc />
    public static bool IsPositiveInfinity(Vec4<TNum> value)
        => TNum.IsPositiveInfinity(value.Sum);

    /// <inheritdoc />
    public static bool IsRealNumber(Vec4<TNum> value)
        => TNum.IsRealNumber(value.Sum);

    /// <inheritdoc />
    public static bool IsSubnormal(Vec4<TNum> value)
        => TNum.IsSubnormal(value.Sum);

    /// <inheritdoc />
    public static bool IsZero(Vec4<TNum> value)
        => value == Zero;


    /// <inheritdoc />
    public static int Radix => TNum.Radix;

    /// <inheritdoc />
    public static Vec4<TNum> Atan2(Vec4<TNum> y, Vec4<TNum> x)
        => new(TNum.Atan2(y.X, x.X), TNum.Atan2(y.Y, x.Y), TNum.Atan2(y.Z, x.Z), TNum.Atan2(y.W, x.W));

    /// <inheritdoc />
    public static Vec4<TNum> Atan2Pi(Vec4<TNum> y, Vec4<TNum> x)
        => new(TNum.Atan2Pi(y.X, x.X), TNum.Atan2Pi(y.Y, x.Y), TNum.Atan2Pi(y.Z, x.Z), TNum.Atan2Pi(y.W, x.W));


    /// <inheritdoc />
    public static Vec4<TNum> BitDecrement(Vec4<TNum> x)
        => new(TNum.BitDecrement(x.X), TNum.BitDecrement(x.Y), TNum.BitDecrement(x.Z), TNum.BitDecrement(x.W));

    /// <inheritdoc />
    public static Vec4<TNum> BitIncrement(Vec4<TNum> x)
        => new(TNum.BitIncrement(x.X), TNum.BitIncrement(x.Y), TNum.BitIncrement(x.Z), TNum.BitIncrement(x.W));

    /// <inheritdoc />
    public static Vec4<TNum> FusedMultiplyAdd(Vec4<TNum> l, Vec4<TNum> r, Vec4<TNum> addend)
        => new(TNum.FusedMultiplyAdd(l.X, r.X, addend.X),
            TNum.FusedMultiplyAdd(l.Y, r.Y, addend.Y),
            TNum.FusedMultiplyAdd(l.Z, r.Z, addend.Z),
            TNum.FusedMultiplyAdd(l.W, r.W, addend.W));

    /// <inheritdoc />
    public static Vec4<TNum> Ieee754Remainder(Vec4<TNum> left, Vec4<TNum> right)
        => new(TNum.Ieee754Remainder(left.X, right.X),
            TNum.Ieee754Remainder(left.Y, right.Y),
            TNum.Ieee754Remainder(left.Z, right.Z),
            TNum.Ieee754Remainder(left.W, right.W));

    /// <inheritdoc />
    public static int ILogB(Vec4<TNum> x)
        => TNum.ILogB(x.Sum);

    /// <inheritdoc />
    public static Vec4<TNum> ScaleB(Vec4<TNum> x, int n)
        => new(TNum.ScaleB(x.X, n), TNum.ScaleB(x.Y, n), TNum.ScaleB(x.Z, n), TNum.ScaleB(x.W, n));


    public static Vec4<TNum> Min(Vec4<TNum> l, Vec4<TNum> r)
        => new(TNum.Min(l.X, r.X), TNum.Min(l.Y, r.Y), TNum.Min(l.Z, r.Z), TNum.Min(l.W, r.W));

    public static Vec4<TNum> Max(Vec4<TNum> l, Vec4<TNum> r)
        => new(TNum.Max(l.X, r.X), TNum.Max(l.Y, r.Y), TNum.Max(l.Z, r.Z), TNum.Max(l.W, r.W));

    public static Vec4<TNum> Clamp(Vec4<TNum> vec, Vec4<TNum> min, Vec4<TNum> max)
        => new(TNum.Clamp(vec.X, min.X, max.X),
            TNum.Clamp(vec.Y, min.Y, max.Y),
            TNum.Clamp(vec.Z, min.Z, max.Z),
            TNum.Clamp(vec.W, min.W, max.W));


    /// <inheritdoc />
    public static Vec4<TNum> MaxMagnitude(Vec4<TNum> x, Vec4<TNum> y)
    {
        var xMagnitude = x.SquaredLength;
        var yMagnitude = y.SquaredLength;
        return xMagnitude > yMagnitude ? x : y;
    }


    /// <inheritdoc />
    public static Vec4<TNum> MaxMagnitudeNumber(Vec4<TNum> x, Vec4<TNum> y)
    {
        var xMagnitude = x.SquaredLength;
        var yMagnitude = y.SquaredLength;
        if (TNum.IsNaN(xMagnitude)) return y;
        if (TNum.IsNaN(yMagnitude)) return x;
        return xMagnitude > yMagnitude ? x : y;
    }

    /// <inheritdoc />
    public static Vec4<TNum> MinMagnitude(Vec4<TNum> x, Vec4<TNum> y)
    {
        var xMagnitude = x.SquaredLength;
        var yMagnitude = y.SquaredLength;
        if (TNum.IsNaN(xMagnitude)) return y;
        if (TNum.IsNaN(yMagnitude)) return x;
        return xMagnitude < yMagnitude ? x : y;
    }

    /// <inheritdoc />
    public static Vec4<TNum> MinMagnitudeNumber(Vec4<TNum> x, Vec4<TNum> y)
    {
        var xMagnitude = x.SquaredLength;
        var yMagnitude = y.SquaredLength;
        if (TNum.IsNaN(xMagnitude)) return y;
        if (TNum.IsNaN(yMagnitude)) return x;
        return xMagnitude < yMagnitude ? x : y;
    }

    /// <inheritdoc />
    public int CompareTo(object? obj)
        => obj is not Vec4<TNum> v ? 1 : CompareTo(v);


    /// <inheritdoc />
    public static Vec4<TNum> Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider = null)
        => TryParse(s, style, provider, out var result) ? result : ThrowHelper.ThrowFormatException<Vec4<TNum>>();

    /// <inheritdoc />
    public static Vec4<TNum> Parse(string s, NumberStyles style, IFormatProvider? provider = null)
        => TryParse(s, style, provider, out var result) ? result : ThrowHelper.ThrowFormatException<Vec4<TNum>>();

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider,
        out Vec4<TNum> result)
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
        out Vec4<TNum> result)
    {
        result = default;
        return s is not null && TryParse(s.AsSpan(), style, provider, out result!);
    }

    /// <inheritdoc />
    public static Vec4<TNum> Parse(string s, IFormatProvider? provider = null)
        => TryParse(s, NumberStyles.Any, provider, out var result) ? result : ThrowHelper.ThrowFormatException<Vec4<TNum>>();

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Vec4<TNum> result)
        => TryParse(s, NumberStyles.Any, provider, out result);

    /// <inheritdoc />
    public static Vec4<TNum> Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
        => TryParse(s, NumberStyles.Any, provider, out var result) ? result : ThrowHelper.ThrowFormatException<Vec4<TNum>>();

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Vec4<TNum> result)
        => TryParse(s, NumberStyles.Any, provider, out result);


    public override string ToString()
        => ToString("G", CultureInfo.CurrentCulture);

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        Span<char> buffer = stackalloc char[64]; // plenty for two numbers
        if (TryFormat(buffer, out var charsWritten, format, formatProvider))
            return new string(buffer[..charsWritten]);
        buffer = stackalloc char[128];
        if (TryFormat(buffer, out charsWritten, format, formatProvider))
            return new string(buffer[..charsWritten]);
        return ThrowHelper.ThrowInvalidOperationException<string>();
    }


    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format,
        IFormatProvider? provider = null)
        => ArrayParser.TryFormat(AsSpan(), destination, out charsWritten, format, provider);

    [Pure]
    public TNum AngleTo(Vec4<TNum> other) => AngleBetween(this, other);
    [Pure]
    public static TNum AngleBetween(Vec4<TNum> a, Vec4<TNum> b)
    {
        a = a.Normalized();
        b = b.Normalized();
        var dot = a.Dot(b);
        return TNum.Acos(dot);
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec4<TNum> Normalize(Vec4<TNum> vec) => vec/vec.Length;
    
    
    [Pure]
    public Vec4<TNum> WithElement(int index, TNum elem)
    {
        if(3u<(uint)index)
            IndexThrowHelper.Throw();
        var copy = this;
        ref var xRef=ref Unsafe.AsRef(in copy.X);
        Unsafe.AddByteOffset(ref xRef, Unsafe.SizeOf<TNum>() * index) = elem;
        return copy;
    }
}