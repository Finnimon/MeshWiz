using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Vec3<TNum> : IVec3<Vec3<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly TNum X, Y, Z;
    public TNum ComponentSum => Sum(this);

    public static int ByteSize => Unsafe.SizeOf<TNum>() * Dimensions;


    [Pure]
    public Vec3<TNum> Normalized() => this / Length;

    /// <inheritdoc />
    public static Vec3<TNum> Create<TOtherNum>(TOtherNum other) where TOtherNum : INumberBase<TOtherNum>
        => Create(TNum.CreateTruncating(other));

    static int IVecBase<Vec3<TNum>, TNum>.Dimensions => Dimensions;
    public const int Dimensions = 3;
    int IReadOnlyCollection<TNum>.Count => Dimensions;
    public const int Count = Dimensions;
    [Pure] public TNum Length => TNum.Sqrt(X * X + Y * Y + Z * Z);

    [Pure]
    public TNum SquaredLength
        => X * X + Y * Y + Z * Z;

    public TNum AlignedCuboidVolume => TNum.Abs(X * Y * Z);

    [Obsolete]
    public Vec3(TNum value) => this = Create(value, value, value);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3<TNum> Create(TNum x, TNum y, TNum z)
    {
        Unsafe.SkipInit(out Vec3<TNum> v);
        Unsafe.AsRef(in v.X) = x;
        Unsafe.AsRef(in v.Y) = y;
        Unsafe.AsRef(in v.Z) = z;
        return v;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3<TNum> FromComponents<TList>(TList components)
        where TList : IReadOnlyList<TNum>
        => Create(components[0], components[1], components[2]);


    /// <inheritdoc />
    public static Vec3<TNum> FromComponentsConstrained<TList, TOtherNum>(TList components)
        where TList : IReadOnlyList<TOtherNum> where TOtherNum : INumberBase<TOtherNum>
        => components.Count switch
        {
            0 => Zero,
            1 => Create(TNum.CreateTruncating(components[0]), TNum.Zero, TNum.Zero),
            2 => Create(TNum.CreateTruncating(components[0]), TNum.CreateTruncating(components[1]), TNum.Zero),
            _ => Create(TNum.CreateTruncating(components[0]), TNum.CreateTruncating(components[1]), TNum.CreateTruncating(components[2]))
        };

    /// <inheritdoc />
    public static Vec3<TNum> FromComponentsConstrained<TList>(TList components) where TList : IReadOnlyList<TNum>
        => components.Count switch
        {
            0 => Zero,
            1 => Create(components[0], TNum.Zero, TNum.Zero),
            2 => Create(components[0], components[1], TNum.Zero),
            _ => Create(components[0], components[1], components[2])
        };

    [Obsolete]
    public Vec3(TNum x, TNum y, TNum z) => this = Create(x, y, z);

    /// <inheritdoc />
    public static Vec3<TNum> Create(TNum value)
        => Create(value, value, value);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3<TNum> FromComponents<TList, TOtherNum>(TList components)
        where TList : IReadOnlyList<TOtherNum>
        where TOtherNum : INumberBase<TOtherNum>
        => Create(TNum.CreateTruncating(components[0]),
            TNum.CreateTruncating(components[1]),
            TNum.CreateTruncating(components[2]));


    public Vec3<TNum> Inverse => Invert(this);


    public Vec3<TNum> ZYX => Create(Z, Y, Y);
    public Vec3<TNum> YZX => Create(Y, Z, Y);
    public Vec3<TNum> YXZ => Create(Y, Y, Z);
    public Vec3<TNum> XZY => Create(Y, Z, Y);
    public Vec3<TNum> ZXY => Create(Z, Y, Y);
    public Vec3<TNum> XXX => Create(X);
    public Vec3<TNum> YYY => Create(Y);
    public Vec3<TNum> ZZZ => Create(Z);

    public static Vec3<TNum> Zero => default;


    public static Vec3<TNum> One => Create(TNum.One, TNum.One, TNum.One);

    public static Vec3<TNum> NaN => Create(TNum.NaN, TNum.NaN, TNum.NaN);

    /// <inheritdoc />
    public static Vec3<TNum> NegativeInfinity => Create(TNum.NegativeInfinity);

    /// <inheritdoc />
    public static Vec3<TNum> NegativeZero => Create(TNum.NegativeZero);

    /// <inheritdoc />
    public static Vec3<TNum> PositiveInfinity => Create(TNum.PositiveInfinity);

    public static Vec3<TNum> UnitX => Create(TNum.One, TNum.Zero, TNum.Zero);

    public static Vec3<TNum> UnitY => Create(TNum.Zero, TNum.One, TNum.Zero);

    public static Vec3<TNum> UnitZ => Create(TNum.Zero, TNum.Zero, TNum.One);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3<TOther> To<TOther>() where TOther : unmanaged, IFloatingPointIeee754<TOther>
        => Vec3<TOther>.Create(TOther.CreateTruncating(X), TOther.CreateTruncating(Y), TOther.CreateTruncating(Z));

    public static implicit operator Vector3(Vec3<TNum> v)
        => Vector3.Create(float.CreateTruncating(v.X), float.CreateTruncating(v.Y), float.CreateTruncating(v.Z));

    public static implicit operator Vec3<TNum>(Vector3 v)
        => Create(TNum.CreateTruncating(v.X), TNum.CreateTruncating(v.Y), TNum.CreateTruncating(v.Z));

    public static implicit operator Vec3<TNum>(Vec3<float> v) => v.To<TNum>();
    public static implicit operator Vec3<TNum>(Vec3<double> v) => v.To<TNum>();
    public static implicit operator Vec3<TNum>(Vec3<Half> v) => v.To<TNum>();
    public static implicit operator Vec3<float>(Vec3<TNum> v) => v.To<float>();
    public static implicit operator Vec3<double>(Vec3<TNum> v) => v.To<double>();
    public static implicit operator Vec3<Half>(Vec3<TNum> v) => v.To<Half>();


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3<TNum> operator +(Vec3<TNum> left, Vec3<TNum> right)
        => Create(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3<TNum> operator -(Vec3<TNum> left, Vec3<TNum> right)
        => Create(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    // [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static TNum operator *(Vec3<TNum> left, Vec3<TNum> right)
    //     => left.X * right.X + left.Y * right.Y + left.Z * right.Z;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3<TNum> operator *(Vec3<TNum> vec, TNum scalar)
        => Create(x: vec.X * scalar, y: vec.Y * scalar, z: vec.Z * scalar);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3<TNum> operator *(TNum scalar, Vec3<TNum> vec)
        => Create(vec.X * scalar, vec.Y * scalar, vec.Z * scalar);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3<TNum> operator -(Vec3<TNum> vec) => Create(-vec.X, -vec.Y, -vec.Z);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3<TNum> operator /(Vec3<TNum> vec, TNum divisor)
        => vec * (TNum.One / divisor);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3<TNum> operator /(TNum num, Vec3<TNum> vec)
        => Create(num / vec.X, num / vec.Y, num / vec.Z);

    [Pure, SuppressMessage("ReSharper", "CompareOfTNumsByEqualityOperator"),
     MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vec3<TNum> left, Vec3<TNum> right)
        => left.X == right.X && left.Y == right.Y && left.Z == right.Z;

    [Pure, SuppressMessage("ReSharper", "CompareOfTNumsByEqualityOperator"),
     MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vec3<TNum> left, Vec3<TNum> right)
        => left.X != right.X || left.Y != right.Y || left.Z != right.Z;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3<TNum> Add(Vec3<TNum> other)
        => this + other;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3<TNum> Subtract(Vec3<TNum> other)
        => this - other;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3<TNum> Scale(TNum scalar)
        => this * scalar;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3<TNum> Divide(TNum divisor)
        => this / divisor;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum Dot(Vec3<TNum> other)
        => Dot(this, other);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TNum Dot(Vec3<TNum> a, Vec3<TNum> b) => Sum(a * b);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TNum Sum(Vec3<TNum> v) => v.X + v.Y + v.Z;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum DistanceTo(Vec3<TNum> other) => Distance(this, other);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum SquaredDistanceTo(Vec3<TNum> other) => SquaredDistance(this, other);

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TNum Distance(Vec3<TNum> a, Vec3<TNum> b) => TNum.Sqrt(SquaredDistance(a, b));

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TNum SquaredDistance(Vec3<TNum> a, Vec3<TNum> b)
    {
        var x = a.X - b.X;
        var y = a.Y - b.Y;
        var z = a.Z - b.Z;
        return x * x + y * y + z * z;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3<TNum> Cross(Vec3<TNum> a, Vec3<TNum> b)
        => Create(x: a.Y * b.Z - a.Z * b.Y,
            y: a.Z * b.X - a.X * b.Z,
            z: a.X * b.Y - a.Y * b.X);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3<TNum> Cross(Vec3<TNum> other) => Cross(this, other);


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsParallelTo(Vec3<TNum> other, TNum tolerance)
        => (TNum.Abs(Dot(Normalized(), other.Normalized()))).IsApprox(TNum.One, tolerance);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsParallelTo(Vec3<TNum> other)
    {
        var dot = TNum.Abs(Dot(this.Normalized(), other.Normalized())) - TNum.One;
        return dot.IsApproxZero();
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Vec3<TNum> other)
        => this == other;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(Vec3<TNum> other)
        => SquaredLength.CompareTo(other.SquaredLength);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<TNum> AsSpan() => MemoryMarshal.CreateReadOnlySpan(in X, Dimensions);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? other)
        => other is Vec3<TNum> vec && vec == this;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    [Pure]
    public TNum this[int index]
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (Dimensions <= (uint)index) IndexThrowHelper.Throw(index, Dimensions);
            return Vec<TNum>.GetElement(in this, index);
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

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out TNum x, out TNum y, out TNum z)
    {
        x = X;
        y = Y;
        z = Z;
    }


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3<TNum> Lerp(Vec3<TNum> a, Vec3<TNum> b, TNum t)
    {
        var negT = TNum.One - t;
        return Create(
            x: b.X * t + a.X * negT,
            y: b.Y * t + a.Y * negT,
            z: b.Z * t + a.Z * negT
        );
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3<TNum> Lerp(Vec3<TNum> a, Vec3<TNum> b, Vec3<TNum> t)
        => Create(x: b.X * t.X + a.X * (TNum.One - t.X),
            y: b.Y * t.Y + a.Y * (TNum.One - t.Y),
            z: b.Z * t.Z + a.Z * (TNum.One - t.Z)
        );


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3<TNum> ExactLerp(Vec3<TNum> from, Vec3<TNum> toward, TNum exactDistance)
    {
        var len = toward.DistanceTo(from);
        var lerpFactor = exactDistance / len;
        return Lerp(from, toward, lerpFactor);
    }


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3<TNum> CosineLerp(Vec3<TNum> from, Vec3<TNum> to, TNum normalDistance)
    {
        var two = TNum.CreateTruncating(2);
        normalDistance = normalDistance.Wrap(TNum.Zero, two);
        var sineDistance = TNum.Sin(normalDistance * TNum.Pi / two);
        sineDistance = TNum.Clamp(sineDistance, TNum.Zero, TNum.One);
        return Lerp(from, to, sineDistance);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3<TNum> Invert(Vec3<TNum> vec)
        => TNum.One / vec;

    public static Vec3<TNum> Pow(Vec3<TNum> v, TNum power)
        => Create(TNum.Pow(v.X, power),
            TNum.Pow(v.Y, power),
            TNum.Pow(v.Z, power));

    public static Vec3<TNum> Squared(Vec3<TNum> v)
        => Create(v.X * v.X, v.Y * v.Y, v.Z * v.Z);

    public static Vec3<TNum> ElementWiseMul(Vec3<TNum> l, Vec3<TNum> r)
        => Create(l.X * r.X, l.Y * r.Y, l.Z * r.Z);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3<TNum> Min(Vec3<TNum> left, Vec3<TNum> right)
        => Create(TNum.Min(left.X, right.X), TNum.Min(left.Y, right.Y), TNum.Min(left.Z, right.Z));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3<TNum> Max(Vec3<TNum> left, Vec3<TNum> right)
        => Create(TNum.Max(left.X, right.X), TNum.Max(left.Y, right.Y), TNum.Max(left.Z, right.Z));

    /// <inheritdoc />
    public int CompareTo(object? obj) => obj is Vec3<TNum> vec ? CompareTo(vec) : 1;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage("ReSharper", "EqualExpressionComparison")]
    // [SuppressMessage("ReSharper", "EqualExpressionComparison")]
    public static bool IsNaN(Vec3<TNum> vec)
#pragma warning disable CS1718
        => vec != vec;
#pragma warning restore CS1718

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ray3<TNum> RayThrough(Vec3<TNum> through) => new Ray3<TNum>(this, through - this);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Line<Vec3<TNum>, TNum> LineTo(Vec3<TNum> end) => new(this, end);


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsApprox(Vec3<TNum> other, TNum squareTolerance) => SquaredDistanceTo(other) < squareTolerance;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsApprox(Vec3<TNum> other) => (this - other).IsApproxZero();


    /// <inheritdoc />
    public static Vec3<TNum> operator %(Vec3<TNum> l, Vec3<TNum> r)
        => Create(l.X % r.X, l.Y % r.Y, l.Z % r.Z);

    /// <inheritdoc />
    public static Vec3<TNum> operator +(Vec3<TNum> v)
        => Create(+v.X, +v.Y, +v.Z);

    /// <inheritdoc />
    public static Vec3<TNum> operator *(Vec3<TNum> left, Vec3<TNum> right)
        => Create(left.X * right.X, left.Y * right.Y, left.Z * right.Z);

    /// <inheritdoc />
    public static Vec3<TNum> Pow(Vec3<TNum> x, Vec3<TNum> y)
        => Create(TNum.Pow(x.X, y.X), TNum.Pow(x.Y, y.Y), TNum.Pow(x.Z, y.Z));

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3<TNum> Abs(Vec3<TNum> v)
        => Create(TNum.Abs(v.X), TNum.Abs(v.Y), TNum.Abs(v.Z));

    /// <inheritdoc />
    public static bool IsCanonical(Vec3<TNum> value)
        => TNum.IsCanonical(value.ComponentSum);

    /// <inheritdoc />
    public static bool IsComplexNumber(Vec3<TNum> v)
        => TNum.IsComplexNumber(v.ComponentSum);

    /// <inheritdoc />
    public static bool IsEvenInteger(Vec3<TNum> v)
        => TNum.IsEvenInteger(v.ComponentSum);

    /// <inheritdoc />
    public static bool IsFinite(Vec3<TNum> v)
        => TNum.IsFinite(v.ComponentSum);

    /// <inheritdoc />
    public static bool IsImaginaryNumber(Vec3<TNum> value)
        => TNum.IsImaginaryNumber(value.ComponentSum);

    /// <inheritdoc />
    public static bool IsInfinity(Vec3<TNum> value)
        => TNum.IsInfinity(value.ComponentSum);

    /// <inheritdoc />
    public static bool IsInteger(Vec3<TNum> value)
        => TNum.IsInteger(value.ComponentSum);

    /// <inheritdoc />
    public static Vec3<TNum> AdditiveIdentity => Zero;


    /// <inheritdoc />
    public static bool operator >(Vec3<TNum> left, Vec3<TNum> right)
    {
        var max = Max(left, right);
        return max == left && max != right;
    }

    /// <inheritdoc />
    public static bool operator >=(Vec3<TNum> left, Vec3<TNum> right)
        => Max(left, right) == left;

    /// <inheritdoc />
    public static bool operator <(Vec3<TNum> left, Vec3<TNum> right)
        => right > left;

    /// <inheritdoc />
    public static bool operator <=(Vec3<TNum> left, Vec3<TNum> right)
        => right >= left;

    /// <inheritdoc />
    public static Vec3<TNum> operator --(Vec3<TNum> v)
        => v - One;

    /// <inheritdoc />
    public static Vec3<TNum> operator /(Vec3<TNum> l, Vec3<TNum> r)
        => Create(l.X / r.X, l.Y / r.Y, l.Z / r.Z);

    /// <inheritdoc />
    public static Vec3<TNum> operator ++(Vec3<TNum> v)
        => v + One;

    /// <inheritdoc />
    public static Vec3<TNum> MultiplicativeIdentity => One;

    /// <inheritdoc />
    public static Vec3<TNum> E => Create(TNum.E);

    /// <inheritdoc />
    public static Vec3<TNum> Pi => Create(TNum.Pi);

    /// <inheritdoc />
    public static Vec3<TNum> Tau => Create(TNum.Tau);

    /// <inheritdoc />
    public static Vec3<TNum> Exp(Vec3<TNum> v)
        => Create(TNum.Exp(v.X), TNum.Exp(v.Y), TNum.Exp(v.Z));

    /// <inheritdoc />
    public static Vec3<TNum> Exp10(Vec3<TNum> v)
        => Create(TNum.Exp10(v.X), TNum.Exp10(v.Y), TNum.Exp10(v.Z));

    /// <inheritdoc />
    public static Vec3<TNum> Exp2(Vec3<TNum> v)
        => Create(TNum.Exp2(v.X), TNum.Exp2(v.Y), TNum.Exp2(v.Z));


    /// <inheritdoc />
    public static Vec3<TNum> NegativeOne => Create(TNum.NegativeOne);


    /// <inheritdoc />
    public static Vec3<TNum> Round(Vec3<TNum> vec, int digits, MidpointRounding mode)
        => Create(TNum.Round(vec.X, digits, mode),
            TNum.Round(vec.Y, digits, mode),
            TNum.Round(vec.Z, digits, mode));


    /// <inheritdoc />
    public static Vec3<TNum> Acosh(Vec3<TNum> vec)
        => Create(TNum.Acosh(vec.X), TNum.Acosh(vec.Y), TNum.Acosh(vec.Z));

    /// <inheritdoc />
    public static Vec3<TNum> Asinh(Vec3<TNum> vec)
        => Create(TNum.Asinh(vec.X), TNum.Asinh(vec.Y), TNum.Asinh(vec.Z));

    /// <inheritdoc />
    public static Vec3<TNum> Atanh(Vec3<TNum> vec)
        => Create(TNum.Atanh(vec.X), TNum.Atanh(vec.Y), TNum.Atanh(vec.Z));

    /// <inheritdoc />
    public static Vec3<TNum> Cosh(Vec3<TNum> vec)
        => Create(TNum.Cosh(vec.X), TNum.Cosh(vec.Y), TNum.Cosh(vec.Z));


    /// <inheritdoc />
    public static Vec3<TNum> Sinh(Vec3<TNum> vec)
        => Create(TNum.Sinh(vec.X), TNum.Sinh(vec.Y), TNum.Sinh(vec.Z));


    /// <inheritdoc />
    public static Vec3<TNum> Tanh(Vec3<TNum> vec)
        => Create(TNum.Tanh(vec.X), TNum.Tanh(vec.Y), TNum.Tanh(vec.Z));

    /// <inheritdoc />
    public static Vec3<TNum> Log(Vec3<TNum> vec)
        => Create(TNum.Log(vec.X), TNum.Log(vec.Y), TNum.Log(vec.Z));

    /// <inheritdoc />
    public static Vec3<TNum> Log(Vec3<TNum> vec, Vec3<TNum> newBase)
        => Create(TNum.Log(vec.X, newBase.X),
            TNum.Log(vec.Y, newBase.Y),
            TNum.Log(vec.Z, newBase.Z));

    /// <inheritdoc />
    public static Vec3<TNum> Log10(Vec3<TNum> vec)
        => Create(TNum.Log10(vec.X), TNum.Log10(vec.Y), TNum.Log10(vec.Z));

    /// <inheritdoc />
    public static Vec3<TNum> Log2(Vec3<TNum> vec)
        => Create(TNum.Log2(vec.X), TNum.Log2(vec.Y), TNum.Log2(vec.Z));


    /// <inheritdoc />
    public static Vec3<TNum> Cbrt(Vec3<TNum> vec)
        => Create(TNum.Cbrt(vec.X), TNum.Cbrt(vec.Y), TNum.Cbrt(vec.Z));

    /// <inheritdoc />
    public static Vec3<TNum> Hypot(Vec3<TNum> x, Vec3<TNum> y)
        => Create(TNum.Hypot(x.X, y.X), TNum.Hypot(x.Y, y.Y), TNum.Hypot(x.Z, y.Z));


    /// <inheritdoc />
    public static Vec3<TNum> RootN(Vec3<TNum> vec, int n)
        => Create(TNum.RootN(vec.X, n), TNum.RootN(vec.Y, n), TNum.RootN(vec.Z, n));


    /// <inheritdoc />
    public static Vec3<TNum> Sqrt(Vec3<TNum> vec)
        => Create(TNum.Sqrt(vec.X), TNum.Sqrt(vec.Y), TNum.Sqrt(vec.Z));


    /// <inheritdoc />
    public static Vec3<TNum> Acos(Vec3<TNum> vec)
        => Create(TNum.Acos(vec.X), TNum.Acos(vec.Y), TNum.Acos(vec.Z));

    /// <inheritdoc />
    public static Vec3<TNum> AcosPi(Vec3<TNum> vec)
        => Create(TNum.AcosPi(vec.X), TNum.AcosPi(vec.Y), TNum.AcosPi(vec.Z));


    /// <inheritdoc />
    public static Vec3<TNum> Asin(Vec3<TNum> vec)
        => Create(TNum.Asin(vec.X), TNum.Asin(vec.Y), TNum.Asin(vec.Z));


    /// <inheritdoc />
    public static Vec3<TNum> AsinPi(Vec3<TNum> vec)
        => Create(TNum.AsinPi(vec.X), TNum.AsinPi(vec.Y), TNum.AsinPi(vec.Z));


    /// <inheritdoc />
    public static Vec3<TNum> Atan(Vec3<TNum> vec)
        => Create(TNum.Atan(vec.X), TNum.Atan(vec.Y), TNum.Atan(vec.Z));


    /// <inheritdoc />
    public static Vec3<TNum> AtanPi(Vec3<TNum> vec)
        => Create(TNum.AtanPi(vec.X), TNum.AtanPi(vec.Y), TNum.AtanPi(vec.Z));


    /// <inheritdoc />
    public static Vec3<TNum> Cos(Vec3<TNum> vec)
        => Create(TNum.Cos(vec.X), TNum.Cos(vec.Y), TNum.Cos(vec.Z));

    /// <inheritdoc />
    public static Vec3<TNum> CosPi(Vec3<TNum> vec)
        => Create(TNum.CosPi(vec.X), TNum.CosPi(vec.Y), TNum.CosPi(vec.Z));

    /// <inheritdoc />
    public static Vec3<TNum> Sin(Vec3<TNum> vec)
        => Create(TNum.Sin(vec.X), TNum.Sin(vec.Y), TNum.Sin(vec.Z));

    /// <inheritdoc />
    public static (Vec3<TNum> Sin, Vec3<TNum> Cos) SinCos(Vec3<TNum> vec)
        => (Sin(vec), Cos(vec));

    /// <inheritdoc />
    public static (Vec3<TNum> SinPi, Vec3<TNum> CosPi) SinCosPi(Vec3<TNum> x)
        => (SinPi(x), CosPi(x));

    /// <inheritdoc />
    public static Vec3<TNum> SinPi(Vec3<TNum> vec)
        => Create(TNum.SinPi(vec.X), TNum.SinPi(vec.Y), TNum.SinPi(vec.Z));

    /// <inheritdoc />
    public static Vec3<TNum> Tan(Vec3<TNum> vec)
        => Create(TNum.Tan(vec.X), TNum.Tan(vec.Y), TNum.Tan(vec.Z));

    /// <inheritdoc />
    public static Vec3<TNum> TanPi(Vec3<TNum> vec)
        => Create(TNum.TanPi(vec.X), TNum.TanPi(vec.Y), TNum.TanPi(vec.Z));

    /// <inheritdoc />
    public static bool IsNegative(Vec3<TNum> value)
        => TNum.IsNegative(value.ComponentSum);

    /// <inheritdoc />
    public static bool IsNegativeInfinity(Vec3<TNum> value)
        => TNum.IsNegativeInfinity(value.ComponentSum);

    /// <inheritdoc />
    public static bool IsNormal(Vec3<TNum> value)
        => value.SquaredLength <= TNum.One;

    public bool IsNormalized => this.SquaredLength.IsApprox(TNum.One, Numbers<TNum>.ZeroEpsilon);

    /// <inheritdoc />
    public static bool IsOddInteger(Vec3<TNum> value)
        => TNum.IsOddInteger(value.ComponentSum);

    /// <inheritdoc />
    public static bool IsPositive(Vec3<TNum> value)
        => TNum.IsPositive(value.ComponentSum);

    /// <inheritdoc />
    public static bool IsPositiveInfinity(Vec3<TNum> value)
        => TNum.IsPositiveInfinity(value.ComponentSum);

    /// <inheritdoc />
    public static bool IsRealNumber(Vec3<TNum> value)
        => TNum.IsRealNumber(value.X)
           && TNum.IsRealNumber(value.Y)
           && TNum.IsRealNumber(value.Z);

    /// <inheritdoc />
    public static bool IsSubnormal(Vec3<TNum> value)
        => TNum.IsSubnormal(value.X) || TNum.IsSubnormal(value.Y) || TNum.IsSubnormal(value.Z);

    /// <inheritdoc />
    public static bool IsZero(Vec3<TNum> value)
        => value == Zero;

    /// <inheritdoc />
    public static Vec3<TNum> MaxMagnitude(Vec3<TNum> x, Vec3<TNum> y)
    {
        var xMagnitude = x.SquaredLength;
        var yMagnitude = y.SquaredLength;
        return xMagnitude > yMagnitude ? x : y;
    }


    /// <inheritdoc />
    public static Vec3<TNum> MaxMagnitudeNumber(Vec3<TNum> x, Vec3<TNum> y)
    {
        var xMagnitude = x.SquaredLength;
        var yMagnitude = y.SquaredLength;
        if (TNum.IsNaN(xMagnitude)) return y;
        if (TNum.IsNaN(yMagnitude)) return x;
        return xMagnitude > yMagnitude ? x : y;
    }

    /// <inheritdoc />
    public static Vec3<TNum> MinMagnitude(Vec3<TNum> x, Vec3<TNum> y)
    {
        var xMagnitude = x.SquaredLength;
        var yMagnitude = y.SquaredLength;
        if (TNum.IsNaN(xMagnitude)) return y;
        if (TNum.IsNaN(yMagnitude)) return x;
        return xMagnitude < yMagnitude ? x : y;
    }

    /// <inheritdoc />
    public static Vec3<TNum> MinMagnitudeNumber(Vec3<TNum> x, Vec3<TNum> y)
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
    public static Vec3<TNum> Atan2(Vec3<TNum> y, Vec3<TNum> x)
        => Create(TNum.Atan2(y.X, x.X), TNum.Atan2(y.Y, x.Y), TNum.Atan2(y.Z, x.Z));

    /// <inheritdoc />
    public static Vec3<TNum> Atan2Pi(Vec3<TNum> y, Vec3<TNum> x)
        => Create(TNum.Atan2Pi(y.X, x.X), TNum.Atan2Pi(y.Y, x.Y), TNum.Atan2Pi(y.Z, x.Z));


    /// <inheritdoc />
    public static Vec3<TNum> BitDecrement(Vec3<TNum> x)
        => Create(TNum.BitDecrement(x.X), TNum.BitDecrement(x.Y), TNum.BitDecrement(x.Z));

    /// <inheritdoc />
    public static Vec3<TNum> BitIncrement(Vec3<TNum> x)
        => Create(TNum.BitIncrement(x.X), TNum.BitIncrement(x.Y), TNum.BitIncrement(x.Z));

    /// <inheritdoc />
    public static Vec3<TNum> FusedMultiplyAdd(Vec3<TNum> l, Vec3<TNum> r, Vec3<TNum> addend)
        => Create(TNum.FusedMultiplyAdd(l.X, r.X, addend.X),
            TNum.FusedMultiplyAdd(l.Y, r.Y, addend.Y),
            TNum.FusedMultiplyAdd(l.Z, r.Z, addend.Z));

    /// <inheritdoc />
    public static Vec3<TNum> Ieee754Remainder(Vec3<TNum> left, Vec3<TNum> right)
        => Create(TNum.Ieee754Remainder(left.X, right.X),
            TNum.Ieee754Remainder(left.Y, right.Y),
            TNum.Ieee754Remainder(left.Z, right.Z));

    /// <inheritdoc />
    public static int ILogB(Vec3<TNum> x)
        => TNum.ILogB(x.ComponentSum);

    /// <inheritdoc />
    public static Vec3<TNum> ScaleB(Vec3<TNum> x, int n)
        => Create(TNum.ScaleB(x.X, n), TNum.ScaleB(x.Y, n), TNum.ScaleB(x.Z, n));

    /// <inheritdoc />
    public static Vec3<TNum> Epsilon => Create(TNum.Epsilon);


    /// <inheritdoc />
    public static Vec3<TNum> Parse(string s, IFormatProvider? provider = null)
        => Parse(s.AsSpan(), NumberStyles.Any, provider);

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Vec3<TNum> result)
        => TryParse(s, NumberStyles.Any, provider, out result);

    /// <inheritdoc />
    public static Vec3<TNum> Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
        => TryParse(s, provider, out var result) ? result : ThrowHelper.ThrowFormatException<Vec3<TNum>>();

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Vec3<TNum> result)
        => TryParse(s, NumberStyles.Any, provider, out result);

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider,
        out Vec3<TNum> result)
    {
        var components = new TNum[Dimensions];
        var success = ArrayParser.TryParse(s, style, provider, components);
        result = FromComponents(components);
        return success;
    }

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider,
        out Vec3<TNum> result)
    {
        result = default;
        return s is not null && TryParse(s.AsSpan(), style, provider, out result);
    }


    /// <inheritdoc />
    public static Vec3<TNum> Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider = null)
        => TryParse(s, style, provider, out var result) ? result : ThrowHelper.ThrowFormatException<Vec3<TNum>>();


    /// <inheritdoc />
    public static Vec3<TNum> Parse(string s, NumberStyles style, IFormatProvider? provider = null)
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
        return ThrowHelper.ThrowInvalidOperationException<string>();
    }

    [Pure]
    public TNum AngleTo(Vec3<TNum> other) => AngleBetween(this, other);

    [Pure]
    public static TNum AngleBetween(Vec3<TNum> a, Vec3<TNum> b)
    {
        a = a.Normalized();
        b = b.Normalized();
        var dot = a.Dot(b);
        return TNum.Acos(dot);
    }

    [Pure]
    public static TNum SignedAngleBetween(Vec3<TNum> a, Vec3<TNum> b, Vec3<TNum> about)
    {
        var plane = new Plane<TNum>(about, TNum.Zero);
        var a2D = plane.ProjectIntoLocal(a).Normalized();
        var b2D = plane.ProjectIntoLocal(b).Normalized();

        var dp = a2D.Dot(b2D);
        dp = TNum.Clamp(dp, TNum.NegativeOne, TNum.One);
        var angle = TNum.Acos(dp);

        var z = a2D.Cross(b2D);
        return TNum.CopySign(angle, z);
    }


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3<TNum> FusedMultiplyAdd(Vec3<TNum> a, TNum b, Vec3<TNum> addend)
        => Create(TNum.FusedMultiplyAdd(a.X, b, addend.X), TNum.FusedMultiplyAdd(a.Y, b, addend.Y),
            TNum.FusedMultiplyAdd(a.Z, b, addend.Z));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsPerpendicularTo(Vec3<TNum> other) => Dot(other).IsApproxZero();

    [Pure]
    public Vec3<TNum> WithElement(int index, TNum elem)
    {
        if (2u < (uint)index)
            IndexThrowHelper.Throw();
        var copy = this;
        Vec<TNum>.SetElement(in copy, index, elem);
        return copy;
    }
}