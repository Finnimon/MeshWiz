using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[SuppressMessage("ReSharper", "UnassignedReadonlyField")]
public readonly struct Mat2x2<TNum> : IMat<Mat2x2<TNum>, Vec2<TNum>, Vec2<TNum>, TNum>, ISpatialTransform<Vec2<TNum>>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>, ISpatialTransform<Vec2<TNum>>
{
    public const int ColCount = 2, RowCount = 2, Count = 4, Dimensions = 2;
    public static Mat2x2<TNum> Identity => Create(Vec2<TNum>.UnitX, Vec2<TNum>.UnitY);
    public readonly Vec2<TNum> X, Y;
    

// @formatter:off
    public TNum M00 => X.X; public TNum M01 => X.Y; 
    public TNum M10 => Y.X; public TNum M11 => Y.Y; 
// @formatter:on

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mat2x2<TNum> CreateScalar(TNum v) =>
        Create(v, default, default, v);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mat2x2<TNum> Create(TNum m00, TNum m01, TNum m10, TNum m11) =>
        Create(Vec4<TNum>.Create(m00, m01, m10, m11));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mat2x2<TNum> Create(Vec2<TNum> x, Vec2<TNum> y)
    {
        Unsafe.SkipInit(out Mat2x2<TNum> result);
        Unsafe.AsRef(in result.X) = x;
        Unsafe.AsRef(in result.Y) = y;
        return result;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vec2<Vec2<TNum>> AsNested(Mat2x2<TNum> mat) => Unsafe.BitCast<Mat2x2<TNum>, Vec2<Vec2<TNum>>>(mat);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mat2x2<TNum> Create(Vec2<Vec2<TNum>> mat) => Unsafe.BitCast<Vec2<Vec2<TNum>>, Mat2x2<TNum>>(mat);


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vec4<TNum>(Mat2x2<TNum> mat) => AsVec4(mat);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Mat2x2<TNum>(Vec4<TNum> vec) => Create(vec);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mat2x2<TNum> CreateRotation(Angle<TNum> angle)
    {
        var rad = angle.Radians;
        var (sin, cos) = TNum.SinCos(rad);
        var x = Vec2<TNum>.Create(cos, sin);
        return Create(x, x.Left());
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mat2x2<TNum> Create(Vec4<TNum> v) => Unsafe.BitCast<Vec4<TNum>, Mat2x2<TNum>>(v);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec4<TNum> AsVec4(Mat2x2<TNum> mat) => Unsafe.BitCast<Mat2x2<TNum>, Vec4<TNum>>(mat);

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Mat2x2<TNum> other) => AsVec4(this) == AsVec4(other);

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<TNum> AsSpan()
        => MemoryMarshal.CreateReadOnlySpan(in X.X, Count);

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mat2x2<TNum> operator +(Mat2x2<TNum> left, Mat2x2<TNum> right)
        => (AsVec4(left) + AsVec4(right));

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mat2x2<TNum> operator -(Mat2x2<TNum> left, Mat2x2<TNum> right)
        => (AsVec4(left) + AsVec4(right));


    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mat2x2<TNum> Parse(string s, IFormatProvider? provider)
        => Create(Vec2<Vec2<TNum>>.Parse(s, provider));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mat2x2<TNum> FromColumns(Vec2<TNum> c0, Vec2<TNum> c1)
    {
        Unsafe.SkipInit(out Mat2x2<TNum> result);
        Mat<TNum>.SetCol(in result, 0, c0);
        Mat<TNum>.SetCol(in result, 1, c1);
        return result;
    }

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Mat2x2<TNum> result)
    {
        var suc = Vec2<Vec2<TNum>>.TryParse(s, provider, out var vec);
        result = Create(vec);
        return suc;
    }


    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mat2x2<TNum> Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        => Create(Vec2<Vec2<TNum>>.Parse(s, provider));

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Mat2x2<TNum> result)
    {
        var suc = Vec2<Vec2<TNum>>.TryParse(s, provider, out var vec);
        result = Create(vec);
        return suc;
    }

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        AsNested(this).ToString(format, formatProvider);

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format,
        IFormatProvider? provider) =>
        AsNested(this).TryFormat(destination, out charsWritten, format, provider);

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec2<TNum> GetCol(int column) => Mat<TNum>.GetCol<Mat2x2<TNum>, Vec2<TNum>>(in this, column);

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec2<TNum> GetRow(int row) => Mat<TNum>.GetRow<Mat2x2<TNum>, Vec2<TNum>>(in this, row);

    /// <inheritdoc />
    static int IMat<Mat2x2<TNum>, Vec2<TNum>, Vec2<TNum>, TNum>.RowCount => RowCount;

    /// <inheritdoc />
    static int IMat<Mat2x2<TNum>, Vec2<TNum>, Vec2<TNum>, TNum>.ColCount => ColCount;

    /// <inheritdoc />
    public TNum Det => M00 * M11 - M01 * M10;

    /// <inheritdoc />
    public TNum this[int row, int col] => Mat<TNum>.GetElement<Mat2x2<TNum>, Vec2<TNum>>(this, row, col);

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe TNum[,] ToArrayFast()
    {
        var result = new TNum[RowCount, ColCount];
        fixed (void* sourcePtr = &this)
        fixed (TNum* destPtr = &result[0, 0])
        {
            var bytes = (long)RowCount * ColCount * sizeof(TNum);
            Buffer.MemoryCopy(sourcePtr, destPtr, bytes, bytes);
        }

        return result;
    }

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mat2x2<TNum> operator *(TNum l, Mat2x2<TNum> r) => l * AsVec4(r);

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mat2x2<TNum> operator *(Mat2x2<TNum> l, TNum r) => AsVec4(l) * r;

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Mat2x2<TNum> left, Mat2x2<TNum> right) => AsVec4(left) == AsVec4(right);

    /// <inheritdoc />
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Mat2x2<TNum> left, Mat2x2<TNum> right) => AsVec4(left) != AsVec4(right);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Mat2x2<TNum> Transpose() => Transpose(this);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mat2x2<TNum> Transpose(Mat2x2<TNum> source) => FromColumns(source.X, source.Y);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mat2x2<TNum> operator *(Mat2x2<TNum> l, Mat2x2<TNum> r)
    {
        r = Transpose(r);
        return Create(
            l.X.Dot(r.X), l.X.Dot(r.Y),
            l.Y.Dot(r.X), l.Y.Dot(r.Y)
        );
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec2<TNum> operator *(Mat2x2<TNum> l, Vec2<TNum> r) => l.X * r.X + l.Y * r.Y;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec2<TNum> operator *(Vec2<TNum> vec, Mat2x2<TNum> m) => Transpose(m) * vec;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is Mat2x2<TNum> other && this == other;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => AsNested(this).GetHashCode();

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec2<TNum> TransformPoint(Vec2<TNum> p) => this * p;
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec2<TNum> TransformDirection(Vec2<TNum> p) => this * p;

    /// <inheritdoc />
    public bool IsAffine => Det.IsApprox(TNum.One);
}