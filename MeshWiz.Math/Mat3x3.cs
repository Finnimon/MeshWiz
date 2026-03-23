using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.RefLinq;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
// ReSharper disable once InconsistentNaming
public readonly struct Mat3x3<TNum> : IMat<Mat3x3<TNum>, Vec3<TNum>, Vec3<TNum>, TNum>, ISpatialTransform<Vec3<TNum>>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    /// <inheritdoc />
    public Vec3<TNum> GetCol(int column)
        => Mat<TNum>.GetCol<Mat3x3<TNum>, Vec3<TNum>, Vec3<TNum>>(in this, column);


    /// <inheritdoc />
    public Vec3<TNum> GetRow(int row)
        => Mat<TNum>.GetRow<Mat3x3<TNum>, Vec3<TNum>, Vec3<TNum>>(in this, row);

    [Pure]
    public static Mat3x3<TNum> CreateRotation(Vec3<TNum> axis, Angle<TNum> angle) => Quaternion<TNum>.CreateFromAxisAngle(axis, angle).AsMat3x3();
    
    [Pure]
    public static Mat3x3<TNum> CreateRotation(Ray3<TNum> axis, Angle<TNum> angle) => Quaternion<TNum>.CreateFromAxisAngle(axis, angle).AsMat3x3();

    public const int ColCount = 3;
    public const int RowCount = 3;
    public const int Count = ColCount * RowCount;
    static int IMat<Mat3x3<TNum>, Vec3<TNum>, Vec3<TNum>, TNum>.RowCount => RowCount;
    static int IMat<Mat3x3<TNum>, Vec3<TNum>, Vec3<TNum>, TNum>.ColCount => ColCount;

    public static Mat3x3<TNum> Identity => Create(Vec3<TNum>.UnitX, Vec3<TNum>.UnitY, Vec3<TNum>.UnitZ);

    public static Mat3x3<TNum> Zero => default;

    public static Mat3x3<TNum> One => Create(TNum.One);

    public TNum Det => Determinant();

        
// @formatter:off
    public TNum M00 => X.X; public TNum M01 => X.Y; public TNum M02 => X.Z;
    public TNum M10 => Y.X; public TNum M11 => Y.Y; public TNum M12 => Y.Z;
    public TNum M20 => Z.X; public TNum M21 => Z.Y; public TNum M22 => Z.Z;
// @formatter:on

    public readonly Vec3<TNum> X, Y, Z;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mat3x3<TNum> Create(TNum v)
    {
        var vec = Vec3<TNum>.Create(v);
        return Create(vec, vec, vec);
    }

    public static Mat3x3<TNum> FromComponents(TNum[] components) =>
        components.Length == 9
            ? Unsafe.As<TNum, Mat3x3<TNum>>(ref components[0])
            : ThrowHelper.ThrowArgumentException<Mat3x3<TNum>>("Components must be of length 9");

    public static Mat3x3<TNum> FromComponents(TNum[,] components) =>
        components is { Length: 9, Rank: 2 }
            ? Unsafe.As<TNum, Mat3x3<TNum>>(ref components[0, 0])
            : ThrowHelper.ThrowArgumentException<Mat3x3<TNum>>("Components must be of length 9");

    public static Mat3x3<TNum> FromComponents(IReadOnlyList<TNum> components)
        => Create(
            components[0], components[1], components[2],
            components[3], components[4], components[5],
            components[6], components[7], components[8]);

    [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
    private static Mat3x3<TNum> Create(
        TNum m00, TNum m01, TNum m02,
        TNum m10, TNum m11, TNum m12,
        TNum m20, TNum m21, TNum m22) =>
        Create(
            Vec3<TNum>.Create(m00, m01, m02),
            Vec3<TNum>.Create(m10, m11, m12),
            Vec3<TNum>.Create(m20, m21, m22)
        );

    [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
    public static Mat3x3<TNum> Create(Vec3<TNum> x, Vec3<TNum> y, Vec3<TNum> z)
    {
        Unsafe.SkipInit<Mat3x3<TNum>>(out var m);
        Unsafe.AsRef(in m.X) = x;
        Unsafe.AsRef(in m.Y) = y;
        Unsafe.AsRef(in m.Z) = z;
        return m;
    }

    public static Mat3x3<TNum> FromColumns(Vec3<TNum> c0, Vec3<TNum> c1, Vec3<TNum> c2)
    {
        Unsafe.SkipInit<Mat3x3<TNum>>(out var m);
        Mat<TNum>.SetCol<Mat3x3<TNum>, Vec3<TNum>, Vec3<TNum>>(in m, 0, c0);
        Mat<TNum>.SetCol<Mat3x3<TNum>, Vec3<TNum>, Vec3<TNum>>(in m, 1, c1);
        Mat<TNum>.SetCol<Mat3x3<TNum>, Vec3<TNum>, Vec3<TNum>>(in m, 2, c2);
        return m;
    }

    public TNum this[int row, int col]
    {
        get
        {
            if (RowCount <= (uint)row || ColCount <= (uint)col) IndexThrowHelper.Throw();
            return Mat<TNum>.GetElement<Mat3x3<TNum>, Vec3<TNum>, Vec3<TNum>>(in this, row, col);
        }
    }

    public TNum Determinant()
        => M00 * M11 * M22
           + M01 * M12 * M20
           + M02 * M10 * M21
           - M02 * M11 * M20
           - M01 * M10 * M22
           - M00 * M12 * M21;

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

    public Vec3<TNum> Solve(Vec3<TNum> b)
    {
        var det = Det;
        if (det == TNum.Zero)
            ThrowHelper.ThrowInvalidOperationException("Matrix is singular and cannot solve.");

        // Cramer's rule
        var d0 = b.X * (M11 * M22 - M12 * M21)
                 - M01 * (b.Y * M22 - M12 * b.Z)
                 + M02 * (b.Y * M21 - M11 * b.Z);

        var d1 =
            M00 * (b.Y * M22 - M12 * b.Z)
            - b.X * (M10 * M22 - M12 * M20)
            + M02 * (M10 * b.Z - b.Y * M20);

        var d2 =
            M00 * (M11 * b.Z - b.Y * M21)
            - M01 * (M10 * b.Z - b.Y * M20)
            + b.X * (M10 * M21 - M11 * M20);

        return Vec3<TNum>.Create(d0 / det, d1 / det, d2 / det);
    }

    public Mat3x3<TNum> Transpose() => Transpose(this);
    public static Mat3x3<TNum> Transpose(Mat3x3<TNum> mat) => FromColumns(mat.X, mat.Y, mat.Z);

    public void Deconstruct(out Vec3<TNum> x, out Vec3<TNum> y, out Vec3<TNum> z)
    {
        x = X;
        y = Y;
        z = Z;
    }

    public Mat3x3<TNum> Inverse()
    {
        var det = Determinant();
        if (TNum.Abs(det) < TNum.Epsilon)
            ThrowHelper.ThrowInvalidOperationException("Matrix is singular and cannot invert.");

        // Cofactors
        var c00 = M11 * M22 - M12 * M21;
        var c01 = -(M10 * M22 - M12 * M20);
        var c02 = M10 * M21 - M11 * M20;

        var c10 = -(M01 * M22 - M02 * M21);
        var c11 = M00 * M22 - M02 * M20;
        var c12 = -(M00 * M21 - M01 * M20);

        var c20 = M01 * M12 - M02 * M11;
        var c21 = -(M00 * M12 - M02 * M10);
        var c22 = M00 * M11 - M01 * M10;

        var adj = Create(
            c00, c10, c20,
            c01, c11, c21,
            c02, c12, c22);

        return adj / det;
    }

    public static bool TryInvert(Mat3x3<TNum> m, out Mat3x3<TNum> result)
    {
        var det = m.Determinant();
        if (det == TNum.Zero)
        {
            result = default;
            return false;
        }

        result = m.Inverse();
        return true;
    }

    public static Mat3x3<TNum> operator *(Mat3x3<TNum> a, Mat3x3<TNum> b)
    {
        b = Transpose(b);
        return Create(
            a.X.Dot(b.X), a.X.Dot(b.Y), a.X.Dot(b.Z),
            a.Y.Dot(b.X), a.Y.Dot(b.Y), a.Y.Dot(b.Z),
            a.Z.Dot(b.X), a.Z.Dot(b.Y), a.Z.Dot(b.Z)
        );
    }

    public Vec3<TNum> Multiply(Vec3<TNum> v)
        => this * v;

    public static Vec3<TNum> operator *(Mat3x3<TNum> m, Vec3<TNum> v) => m.X * v.X + m.Y * v.Y + m.Z * v.Z;

    public static Vec3<TNum> operator *(Vec3<TNum> v, Mat3x3<TNum> m) => Transpose(m) * v;

    public static Mat3x3<TNum> operator *(Mat3x3<TNum> mat, TNum scalar)
        => Create(mat.X * scalar, mat.Y * scalar, mat.Z * scalar);

    public static Mat3x3<TNum> operator /(Mat3x3<TNum> mat, TNum divisor)
        => mat * (TNum.One / divisor);

    public static Mat3x3<TNum> operator *(TNum scalar, Mat3x3<TNum> mat)
        => mat * scalar;

    public static Mat3x3<TNum> operator +(Mat3x3<TNum> left, Mat3x3<TNum> right)
        => Create(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    public static Mat3x3<TNum> operator -(Mat3x3<TNum> left, Mat3x3<TNum> right)
        => Create(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    public static Mat3x3<TNum> operator -(Mat3x3<TNum> mat)
        => Create(-mat.X, -mat.Y, -mat.Z);

    /// <inheritdoc />
    public ReadOnlySpan<TNum> AsSpan() => MemoryMarshal.CreateReadOnlySpan(in X.X, ColCount * RowCount);

    public override bool Equals(object? obj)
        => obj is Mat3x3<TNum> m && this == m;

    public static bool operator ==(Mat3x3<TNum> a, Mat3x3<TNum> b)
        => a.X == b.X && a.Y == b.Y && a.Z == b.Z;

    public static bool operator !=(Mat3x3<TNum> a, Mat3x3<TNum> b) => !(a == b);

    public override int GetHashCode()
        => HashCode.Combine(X, Y, Z);


    public bool Equals(Mat3x3<TNum> other)
        => this == other;


    public static implicit operator Vec3<Vec3<TNum>>(Mat3x3<TNum> matrix) =>
        Unsafe.As<Mat3x3<TNum>, Vec3<Vec3<TNum>>>(ref matrix);

    public static implicit operator Mat3x3<TNum>(Vec3<Vec3<TNum>> matrix) =>
        Unsafe.As<Vec3<Vec3<TNum>>, Mat3x3<TNum>>(ref matrix);

    private Vec3<Vec3<TNum>> Nested() => this;
    public override string ToString() => Nested().ToString();


    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return Nested().ToString(format, formatProvider);
    }

    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        return Nested().TryFormat(destination, out charsWritten, format, provider);
    }

    /// <inheritdoc />
    public static Mat3x3<TNum> Parse(string s, IFormatProvider? provider)
        => Vec3<Vec3<TNum>>.Parse(s, provider);

    /// <inheritdoc />
    public static Mat3x3<TNum> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        Vec3<Vec3<TNum>>.Parse(s, provider);

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Mat3x3<TNum> result)
    {
        result = default;
        return s is { Length: > 0 } && TryParse(s, provider, out result);
    }

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Mat3x3<TNum> result)
    {
        var success = Vec3<Vec3<TNum>>.TryParse(s, provider, out var n);
        result = n;
        return success;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Mat3x3<TOther> To<TOther>()
        where TOther : unmanaged, IFloatingPointIeee754<TOther>
    {
        Unsafe.SkipInit(out Mat3x3<TOther> res);
        var newNums = AsSpan().Select(TOther.CreateTruncating);
        var resSpan = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in res.X.X), ColCount * RowCount);
        newNums.CopyTo(resSpan);
        return res;
    }

    /// <inheritdoc />
    public Vec3<TNum> TransformPoint(Vec3<TNum> p) => this * p;

    /// <inheritdoc />
    public Vec3<TNum> TransformDirection(Vec3<TNum> v)
        => this * v;

    /// <inheritdoc />
    public bool IsAffine => Det.IsApprox(TNum.One);

    public static Mat3x3<TNum> CreateScalar(TNum scalar)
    {
        Mat3x3<TNum> mat = default;
        return Mat<TNum>.WithDiagonal(mat, Vec3<TNum>.Create(scalar));
    }
}