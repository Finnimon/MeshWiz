using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using CommunityToolkit.Diagnostics;
using MeshWiz.RefLinq;
using MeshWiz.Utility;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
[JsonConverter(typeof(MeshWizJsonConverter))]
// ReSharper disable once InconsistentNaming
public readonly struct Mat4x4<TNum> : IMat<Mat4x4<TNum>, Vec4<TNum>, Vec4<TNum>, TNum>,
    IJsonConverterSelfProvider,
    ISpatialTransform<Vec3<TNum>> 
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    static int IMat<Mat4x4<TNum>, Vec4<TNum>, Vec4<TNum>, TNum>.RowCount => RowCount;
    static int IMat<Mat4x4<TNum>, Vec4<TNum>, Vec4<TNum>, TNum>.ColCount => ColCount;
    public const int ColCount = 4;
    public const int RowCount = 4;
    public const int Count = ColCount * RowCount;
    [JsonIgnore] public bool IsIdentity => Mat<TNum>.IsIdentity<Mat4x4<TNum>, Vec4<TNum>>(this);

    public static Mat4x4<TNum> Identity => Create(Vec4<TNum>.UnitX,
        Vec4<TNum>.UnitY,
        Vec4<TNum>.UnitZ,
        Vec4<TNum>.UnitW);


    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public static Mat4x4<TNum> Zero => default;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public static Mat4x4<TNum> One => Create(TNum.One);

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public static Mat4x4<TNum> NaN => Create(TNum.NaN);

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TNum Det => Determinant();

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public Vec4<TNum> Diagonal => new(M00, M11, M22, M33);

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TNum Trace => Vec4<TNum>.Sum(Diagonal);

    [Pure]
    public Mat4x4<TNum> Normalized() => this / Det;

    [JsonInclude] public readonly Vec4<TNum> X, Y, Z, W;

    [JsonIgnore] public TNum M00 => X.X;
    [JsonIgnore] public TNum M01 => X.Y;
    [JsonIgnore] public TNum M02 => X.Z;
    [JsonIgnore] public TNum M03 => X.W;
    [JsonIgnore] public TNum M10 => Y.X;
    [JsonIgnore] public TNum M11 => Y.Y;
    [JsonIgnore] public TNum M12 => Y.Z;
    [JsonIgnore] public TNum M13 => Y.W;
    [JsonIgnore] public TNum M20 => Z.X;
    [JsonIgnore] public TNum M21 => Z.Y;
    [JsonIgnore] public TNum M22 => Z.Z;
    [JsonIgnore] public TNum M23 => Z.W;
    [JsonIgnore] public TNum M30 => W.X;
    [JsonIgnore] public TNum M31 => W.Y;
    [JsonIgnore] public TNum M32 => W.Z;
    [JsonIgnore] public TNum M33 => W.W;


    /// <inheritdoc />
    public ReadOnlySpan<TNum> AsSpan() => MemoryMarshal.CreateReadOnlySpan(in X.X, ColCount * RowCount);

    public Mat4x4(
        TNum m00, TNum m01, TNum m02, TNum m03,
        TNum m10, TNum m11, TNum m12, TNum m13,
        TNum m20, TNum m21, TNum m22, TNum m23,
        TNum m30, TNum m31, TNum m32, TNum m33)
    {
        X = Vec4<TNum>.Create(m00, m01, m02, m03);
        Y = Vec4<TNum>.Create(m10, m11, m12, m13);
        Z = Vec4<TNum>.Create(m20, m21, m22, m23);
        W = Vec4<TNum>.Create(m30, m31, m32, m33);
    }

    public Mat4x4(TNum value)
    {
        this = Create(value);
    }


    public Mat4x4(Vec4<TNum> x, Vec4<TNum> y, Vec4<TNum> z, Vec4<TNum> w)
    {
        this = Create(x, y, z, w);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mat4x4<TNum> Create(TNum v)
    {
        Unsafe.SkipInit(out Mat4x4<TNum> m);
        AsSpanUnsafe(ref m).Fill(v);
        return m;
    }

    private static Span<TNum> AsSpanUnsafe(ref Mat4x4<TNum> m) =>
        MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in m.X.X), Count);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mat4x4<TNum> Create(Vec4<TNum> x, Vec4<TNum> y, Vec4<TNum> z, Vec4<TNum> w)
    {
        Unsafe.SkipInit(out Mat4x4<TNum> res);
        Unsafe.AsRef(in res.X) = x;
        Unsafe.AsRef(in res.Y) = y;
        Unsafe.AsRef(in res.Z) = z;
        Unsafe.AsRef(in res.W) = w;
        return res;
    }


    private static readonly int NumSize = Unsafe.SizeOf<TNum>();

    public TNum this[int row, int col]
    {
        get
        {
            if (RowCount <= (uint)row || ColCount <= (uint)col) IndexThrowHelper.Throw();
            return Unsafe.Add(ref Unsafe.AsRef(in X.X), ColCount * row + col);
        }
    }

    public unsafe TNum[,] ToArrayFast()
    {
        var result = new TNum[4, 4];
        fixed (TNum* sourcePtr = &X.X)
        fixed (TNum* destPtr = &result[0, 0])
        {
            var bytes = (long)4 * 4 * sizeof(TNum);
            Buffer.MemoryCopy(sourcePtr, destPtr, bytes, bytes);
        }

        return result;
    }

    public static unsafe ReadOnlySpan<TNum> AsSpan(in Mat4x4<TNum> matrix)
        => MemoryMarshal.CreateReadOnlySpan(in matrix.X.X, Count);

    public Mat4x4<TNum> Transpose() => Transpose(this);

    public static Mat4x4<TNum> Transpose(Mat4x4<TNum> m) => FromColumns(m.X, m.Y, m.Z, m.W);

    public static Mat4x4<TNum> operator *(Mat4x4<TNum> a, Mat4x4<TNum> b)
    {
        b = Transpose(b);
        return new Mat4x4<TNum>(
            a.X.Dot(b.X), a.X.Dot(b.Y), a.X.Dot(b.Z), a.X.Dot(b.W),
            a.Y.Dot(b.X), a.Y.Dot(b.Y), a.Y.Dot(b.Z), a.Y.Dot(b.W),
            a.Z.Dot(b.X), a.Z.Dot(b.Y), a.Z.Dot(b.Z), a.Z.Dot(b.W),
            a.W.Dot(b.X), a.W.Dot(b.Y), a.W.Dot(b.Z), a.W.Dot(b.W)
        );
    }


    public Vec3<TNum> MultiplyPoint(Vec3<TNum> v) => MultiplyPoint(this, v);

    public static Vec3<TNum> MultiplyPoint(Mat4x4<TNum> m, Vec3<TNum> v)
    {
        var v4 = Vec4<TNum>.Create(v, TNum.One);
        v4 = m * v4;
        return v4.XYZ / v4.W;
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3<TNum> MultiplyDirection(Vec3<TNum> v)
        => MultiplyDirection(this,v);

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3<TNum> MultiplyDirection(Mat4x4<TNum> m, Vec3<TNum> v)
        => m.AsMat3x3() * v;

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec4<TNum> Multiply(Vec4<TNum> v)
        => this * v;

    public static Vec4<TNum> operator *(Mat4x4<TNum> m, Vec4<TNum> v) => m.X * v.X + m.Y * v.Y + m.Z * v.Z + m.W * v.W;

    public static Vec4<TNum> operator *(Vec4<TNum> v, Mat4x4<TNum> m) => Transpose(m) * v;
    public static Vec3<TNum> operator *(Mat4x4<TNum> m, Vec3<TNum> v) => m.MultiplyPoint(v);

    public static Mat4x4<TNum> operator *(Mat4x4<TNum> m, TNum scalar) =>
        new(m.X * scalar, m.Y * scalar, m.Z * scalar, m.W * scalar);

    public static Mat4x4<TNum> operator *(TNum scalar, Mat4x4<TNum> m) => m * scalar;
    public static Mat4x4<TNum> operator /(Mat4x4<TNum> m, TNum divisor) => m * (TNum.One / divisor);

    public static Mat4x4<TNum> operator +(Mat4x4<TNum> l, Mat4x4<TNum> r) =>
        new(l.X + r.X, l.Y + r.Y, l.Z + r.Z, l.W + r.W);

    public static Mat4x4<TNum> operator -(Mat4x4<TNum> m) => new(-m.X, -m.Y, -m.Z, -m.W);

    public static Mat4x4<TNum> operator -(Mat4x4<TNum> l, Mat4x4<TNum> r)
        => new(l.X - r.X, l.Y - r.Y, l.Z - r.Z, l.W - r.W);


    // Row/Column access via bitwise cast
    public Vec4<TNum> GetRow(int row)
        => RowCount > (uint)row
            ? Unsafe.Add(ref Unsafe.AsRef(in X), row)
            : ThrowHelper.ThrowArgumentOutOfRangeException<Vec4<TNum>>(nameof(row));


    public Vec4<TNum> GetCol(int col) => Transpose().GetRow(col);

    // Homogeneous utilities
    public static Vec3<TNum> Homogenize(Vec4<TNum> v)
        => Vec3<TNum>.Create(v.X / v.W, v.Y / v.W, v.Z / v.W);

    public static Vec4<TNum> Dehomogenize(Vec3<TNum> v, TNum w)
        => Vec4<TNum>.Create(v.X, v.Y, v.Z, w);

    [Pure]
    public static Mat4x4<TNum> Lerp(Mat4x4<TNum> from, Mat4x4<TNum> to, TNum t)
        => FromRows(
            Vec4<TNum>.Lerp(from.X, to.X, t),
            Vec4<TNum>.Lerp(from.Y, to.Y, t),
            Vec4<TNum>.Lerp(from.Z, to.Z, t),
            Vec4<TNum>.Lerp(from.W, to.W, t)
        );

    [Pure]
    public static Mat4x4<TNum> CreateShear(
        TNum xy, TNum xz, TNum yx, TNum yz, TNum zx, TNum zy)
        => new(
            TNum.One, xy, xz, TNum.Zero,
            yx, TNum.One, yz, TNum.Zero,
            zx, zy, TNum.One, TNum.Zero,
            TNum.Zero, TNum.Zero, TNum.Zero, TNum.One);

    [Pure]
    public static Mat4x4<TNum> CreateRotation(Vec3<TNum> axis, TNum angle)
    {
        axis = axis.Normalized();

        var cos = TNum.Cos(angle);
        var sin = TNum.Sin(angle);
        var t = TNum.One - cos;
        var tAxis = t * axis;
        var x = axis.XXX * tAxis;
        var y = axis.YYY * tAxis;
        var z = axis.ZZZ * tAxis;
        var (sinX, sinY, sinZ) = sin * axis;
        x += Vec3<TNum>.Create(cos, -sinZ, sinY);
        y += Vec3<TNum>.Create(sinZ, cos, -sinX);
        z += Vec3<TNum>.Create(-sinY, sinX, cos);
        return FromRows(x, y, z, Vec4<TNum>.UnitW);
    }

    public static Mat4x4<TNum> CreateTranslation(Vec3<TNum> translation)
        => Identity + FromRows(Vec4<TNum>.Zero,
            Vec4<TNum>.Zero,
            Vec4<TNum>.Zero,
            translation);

    public static Mat4x4<TNum> CreateScale(TNum scalar)
        => new(scalar, TNum.Zero, TNum.Zero, TNum.Zero,
            TNum.Zero, scalar, TNum.Zero, TNum.Zero,
            TNum.Zero, TNum.Zero, scalar, TNum.Zero,
            TNum.Zero, TNum.Zero, TNum.Zero, TNum.One);

    public static Mat4x4<TNum> CreateScale(Vec3<TNum> scalar)
        => new(scalar.X, TNum.Zero, TNum.Zero, TNum.Zero,
            TNum.Zero, scalar.Y, TNum.Zero, TNum.Zero,
            TNum.Zero, TNum.Zero, scalar.Z, TNum.Zero,
            TNum.Zero, TNum.Zero, TNum.Zero, TNum.One);

    public static Mat4x4<TNum> CreateViewAt(Vec3<TNum> eye, Vec3<TNum> target, Vec3<TNum> up)
    {
        var z = (eye - target).Normalized();
        var x = Vec3<TNum>.Cross(up, z).Normalized();
        var y = Vec3<TNum>.Cross(z, x).Normalized();
        var w = Vec4<TNum>.Create(
            -(x.Dot(eye)),
            -(y.Dot(eye)),
            -(z.Dot(eye)),
            TNum.One
        );
        return FromRows(new Vec4<TNum>(x.X, y.X, z.X, TNum.Zero),
            new Vec4<TNum>(x.Y, y.Y, z.Y, TNum.Zero),
            new Vec4<TNum>(x.Z, y.Z, z.Z, TNum.Zero),
            w);
    }


    public static Mat4x4<TNum> FromTopLeft(Mat3x3<TNum> topleft)
        => new(new Vec4<TNum>(topleft.X), new Vec4<TNum>(topleft.Y), new Vec4<TNum>(topleft.Z),
            new Vec4<TNum>(Vec3<TNum>.Zero, TNum.Zero));

    public static Mat4x4<TNum> FromRows(Vec4<TNum> x, Vec4<TNum> y, Vec4<TNum> z, Vec4<TNum> w)
        => new(x, y, z, w);

    public static Mat4x4<TNum> FromColumns(Vec4<TNum> x, Vec4<TNum> y, Vec4<TNum> z, Vec4<TNum> w)
        => new(x.X, y.X, z.X, w.X,
            x.Y, y.Y, z.Y, w.Y,
            x.Z, y.Z, z.Z, w.Z,
            x.W, y.W, z.W, w.W);

    public override bool Equals(object? obj) => obj is Mat4x4<TNum> m && this == m;

    public static bool operator ==(Mat4x4<TNum> a, Mat4x4<TNum> b) =>
        a.M00 == b.M00 && a.M01 == b.M01 && a.M02 == b.M02 && a.M03 == b.M03 &&
        a.M10 == b.M10 && a.M11 == b.M11 && a.M12 == b.M12 && a.M13 == b.M13 &&
        a.M20 == b.M20 && a.M21 == b.M21 && a.M22 == b.M22 && a.M23 == b.M23 &&
        a.M30 == b.M30 && a.M31 == b.M31 && a.M32 == b.M32 && a.M33 == b.M33;

    public static bool operator !=(Mat4x4<TNum> a, Mat4x4<TNum> b) => !(a == b);

    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);


    public static implicit operator Vec4<Vec4<TNum>>(Mat4x4<TNum> m) =>
        Unsafe.BitCast<Mat4x4<TNum>, Vec4<Vec4<TNum>>>(m);

    public static implicit operator Mat4x4<TNum>(Vec4<Vec4<TNum>> n) =>
        Unsafe.BitCast<Vec4<Vec4<TNum>>, Mat4x4<TNum>>(n);

    private Vec4<Vec4<TNum>> Nested()
        => this;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec4<Vec4<TNum>> AsNested(Mat4x4<TNum> m) => m;

    public TNum Determinant()
    {
        // Compute 3x3 minors
        var det00 = M11 * (M22 * M33 - M23 * M32) - M12 * (M21 * M33 - M23 * M31) + M13 * (M21 * M32 - M22 * M31);
        var det01 = M10 * (M22 * M33 - M23 * M32) - M12 * (M20 * M33 - M23 * M30) + M13 * (M20 * M32 - M22 * M30);
        var det02 = M10 * (M21 * M33 - M23 * M31) - M11 * (M20 * M33 - M23 * M30) + M13 * (M20 * M31 - M21 * M30);
        var det03 = M10 * (M21 * M32 - M22 * M31) - M11 * (M20 * M32 - M22 * M30) + M12 * (M20 * M31 - M21 * M30);
        return
            M00 * det00
            - M01 * det01
            + M02 * det02
            - M03 * det03;
    }


    public bool Equals(Mat4x4<TNum> other)
        => AsNested(this) == AsNested(other);

    public (TNum left,
        TNum right,
        TNum bottom,
        TNum top,
        TNum depthNear,
        TNum depthFar) ExtractPerspectiveOffCenter()
    {
        var depthNear = M32 / (M22 - TNum.One);
        var depthFar = M32 / (M22 + TNum.One);
        var left = depthNear * (M20 - TNum.One) / M00;
        var right = depthNear * (M20 + TNum.One) / M00;
        var bottom = depthNear * (M21 - TNum.One) / M11;
        var top = depthNear * (M21 + TNum.One) / M11;
        return (left, right, bottom, top, depthNear, depthFar);
    }

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
    public static Mat4x4<TNum> Parse(string s, IFormatProvider? provider)
        => Vec4<Vec4<TNum>>.Parse(s, provider);

    /// <inheritdoc />
    public static Mat4x4<TNum> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        Vec4<Vec4<TNum>>.Parse(s, provider);

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Mat4x4<TNum> result)
    {
        result = default;
        return s is { Length: > 0 } && TryParse(s, provider, out result);
    }

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Mat4x4<TNum> result)
    {
        var success = Vec4<Vec4<TNum>>.TryParse(s, provider, out var n);
        result = n;
        return success;
    }


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Mat4x4<TOther> To<TOther>()
        where TOther : unmanaged, IFloatingPointIeee754<TOther>
    {
        Unsafe.SkipInit(out Mat4x4<TOther> res);
        var newNums = AsSpan().Select(TOther.CreateTruncating);
        var resSpan = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in res.X.X), ColCount * RowCount);
        newNums.CopyTo(resSpan);
        return res;
    }

    public static Mat4x4<TNum> CreatePerspectiveFov(TNum fovy, TNum aspect, TNum depthNear, TNum depthFar)
    {
        if (fovy <= TNum.Zero || fovy > TNum.Pi) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(fovy), fovy, null);
        if (aspect <= TNum.Zero) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(aspect), aspect, null);
        if (depthNear <= TNum.Zero) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(depthNear), depthNear, null);
        if (depthFar <= TNum.Zero) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(depthFar), depthFar, null);

        var maxY = depthNear * TNum.Tan(Numbers<TNum>.Half * fovy);
        var minY = -maxY;
        var minX = minY * aspect;
        var maxX = maxY * aspect;

        return CreatePerspectiveOffCenter(minX, maxX, minY, maxY, depthNear, depthFar);
    }

    public static Mat4x4<TNum> CreatePerspectiveOffCenter(
        TNum left,
        TNum right,
        TNum bottom,
        TNum top,
        TNum depthNear,
        TNum depthFar)
    {
        if (depthNear <= TNum.Zero) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(depthNear), depthNear, null);
        if (depthFar <= TNum.Zero) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(depthFar), depthFar, null);
        if (depthNear >= depthFar) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(depthNear));
        var two = Numbers<TNum>.Two;
        var x = two * depthNear / (right - left);
        var y = two * depthNear / (top - bottom);
        var a = (right + left) / (right - left);
        var b = (top + bottom) / (top - bottom);
        var c = -(depthFar + depthNear) / (depthFar - depthNear);
        var d = -(two * depthFar * depthNear) / (depthFar - depthNear);
        return Create(
            Vec4<TNum>.Zero.WithElement(0, x),
            Vec4<TNum>.Zero.WithElement(1, y),
            Vec4<TNum>.Create(a, b, c, TNum.NegativeOne),
            Vec4<TNum>.Zero.WithElement(2, d)
        );
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Mat3x3<TNum> AsMat3x3() => Mat3x3<TNum>.Create(X.XYZ, Y.XYZ, Z.XYZ);
    /// <inheritdoc />
    static JsonConverter IJsonConverterSelfProvider.CreateConverter(JsonSerializerOptions options) 
        => new IMat<Mat4x4<TNum>,Vec4<TNum>,Vec4<TNum>,TNum>.Converter();

    /// <inheritdoc />
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3<TNum> TransformPoint(Vec3<TNum> p) => MultiplyPoint(this, p);

    /// <inheritdoc />
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3<TNum> TransformDirection(Vec3<TNum> v) => MultiplyDirection(this,v);

    /// <inheritdoc />
    bool ISpatialTransform<Vec3<TNum>>.IsAffine => AsMat3x3().IsAffine;
}