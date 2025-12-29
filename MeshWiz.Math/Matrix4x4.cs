using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
// ReSharper disable once InconsistentNaming
public readonly struct Matrix4x4<TNum> : IMatrix<Matrix4x4<TNum>, Vec4<TNum>, Vec4<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public static int RowCount => 4;
    public static int ColCount => 4;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public static Matrix4x4<TNum> Identity
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } =
        new(Vec4<TNum>.UnitX,
            Vec4<TNum>.UnitY,
            Vec4<TNum>.UnitZ,
            Vec4<TNum>.UnitW);


    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public static Matrix4x4<TNum> Zero
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = new(TNum.Zero);

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public static Matrix4x4<TNum> One
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = new(TNum.One);

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public static Matrix4x4<TNum> NaN
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = new(TNum.NaN);

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TNum Det => Determinant();

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public Vec4<TNum> Diagonal => new(M00, M11, M22, M33);

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TNum Trace => M00 + M11 + M22 + M33;

    [Pure]
    public Matrix4x4<TNum> Normalized() => this / Det;

    public readonly Vec4<TNum> X, Y, Z, W;

// @formatter:off
    public TNum M00 => X.X; public TNum M01 => X.Y; public TNum M02 => X.Z; public TNum M03 => X.W;
    public TNum M10 => Y.X; public TNum M11 => Y.Y; public TNum M12 => Y.Z; public TNum M13 => Y.W;
    public TNum M20 => Z.X; public TNum M21 => Z.Y; public TNum M22 => Z.Z; public TNum M23 => Z.W;
    public TNum M30 => W.X; public TNum M31 => W.Y; public TNum M32 => W.Z; public TNum M33 => W.W;
// @formatter:on


    /// <inheritdoc />
    public unsafe ReadOnlySpan<TNum> AsSpan() => new(Unsafe.AsPointer(in this), ColCount * RowCount);

    public Matrix4x4(
        TNum m00, TNum m01, TNum m02, TNum m03,
        TNum m10, TNum m11, TNum m12, TNum m13,
        TNum m20, TNum m21, TNum m22, TNum m23,
        TNum m30, TNum m31, TNum m32, TNum m33)
    {
        X = new(m00, m01, m02, m03);
        Y = new(m10, m11, m12, m13);
        Z = new(m20, m21, m22, m23);
        W = new(m30, m31, m32, m33);
    }

    public Matrix4x4(TNum value)
    {
        X = new(value);
        Y = X;
        Z = Y;
        W = Z;
    }


    public Matrix4x4(Vec4<TNum> x, Vec4<TNum> y, Vec4<TNum> z, Vec4<TNum> w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    private static readonly int NumSize = Unsafe.SizeOf<TNum>();
    public  TNum this[int row, int column]
    {
        get
        {
            if (RowCount <= (uint)row || ColCount <= (uint)column)
                IndexThrowHelper.Throw();
            return Unsafe.AddByteOffset(ref Unsafe.AsRef(in X.X), NumSize * ColCount * row + column * NumSize);
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

    public static unsafe ReadOnlySpan<TNum> AsSpan(in Matrix4x4<TNum> matrix) 
        => new(Unsafe.AsPointer(in matrix), 16);

    public Matrix4x4<TNum> Transpose() => FromColumns(X, Y, Z, W);

    public static Matrix4x4<TNum> Transpose(Matrix4x4<TNum> m) => m.Transpose();

    public static Matrix4x4<TNum> operator *(Matrix4x4<TNum> a, Matrix4x4<TNum> b)
    {
        b = b.Transpose();
        return new Matrix4x4<TNum>(
            a.X.Dot(b.X), a.X.Dot(b.Y), a.X.Dot(b.Z), a.X.Dot(b.W),
            a.Y.Dot(b.X), a.Y.Dot(b.Y), a.Y.Dot(b.Z), a.Y.Dot(b.W),
            a.Z.Dot(b.X), a.Z.Dot(b.Y), a.Z.Dot(b.Z), a.Z.Dot(b.W),
            a.W.Dot(b.X), a.W.Dot(b.Y), a.W.Dot(b.Z), a.W.Dot(b.W)
        );
    }


    public Vec3<TNum> MultiplyPoint(Vec3<TNum> v)
    {
        var v4 = new Vec4<TNum>(v, TNum.One);
        v4 = Multiply(v4);
        return v4.XYZ / v4.W;
    }

    [Pure]
    public Vec3<TNum> MultiplyDirection(Vec3<TNum> v)
        => new(X.XYZ.Dot(v), Y.XYZ.Dot(v), Z.XYZ.Dot(v));

    [Pure]
    public Vec4<TNum> Multiply(Vec4<TNum> v)
        => new(X.Dot(v), Y.Dot(v), Z.Dot(v), W.Dot(v));

    public static Vec4<TNum> operator *(Matrix4x4<TNum> m, Vec4<TNum> v) => m.Multiply(v);
    public static Vec3<TNum> operator *(Matrix4x4<TNum> m, Vec3<TNum> v) => m.MultiplyPoint(v);

    public static Matrix4x4<TNum> operator *(Matrix4x4<TNum> m, TNum scalar) =>
        new(m.X * scalar, m.Y * scalar, m.Z * scalar, m.W * scalar);

    public static Matrix4x4<TNum> operator *(TNum scalar, Matrix4x4<TNum> m) => m * scalar;
    public static Matrix4x4<TNum> operator /(Matrix4x4<TNum> m, TNum divisor) => m * (TNum.One / divisor);

    public static Matrix4x4<TNum> operator +(Matrix4x4<TNum> l, Matrix4x4<TNum> r) =>
        new(l.X + r.X, l.Y + r.Y, l.Z + r.Z, l.W + r.W);

    public static Matrix4x4<TNum> operator -(Matrix4x4<TNum> m) => new(-m.X, -m.Y, -m.Z, -m.W);

    public static Matrix4x4<TNum> operator -(Matrix4x4<TNum> l, Matrix4x4<TNum> r)
        => new(l.X - r.X, l.Y - r.Y, l.Z - r.Z, l.W - r.W);


    // Row/Column access via bitwise cast
    public Vec4<TNum> GetRow(int row) 
        => RowCount > (uint)row
            ? Unsafe.AddByteOffset(ref Unsafe.AsRef(in X), Vec3<TNum>.ByteSize * row)
            : ThrowHelper.ThrowArgumentOutOfRangeException<Vec4<TNum>>(nameof(row));


    public Vec4<TNum> GetCol(int col) => Transpose().GetRow(col);

    // Homogeneous utilities
    public static Vec3<TNum> Homogenize(Vec4<TNum> v)
        => new(v.X / v.W, v.Y / v.W, v.Z / v.W);

    public static Vec4<TNum> Dehomogenize(Vec3<TNum> v, TNum w)
        => new(v.X, v.Y, v.Z, w);

    [Pure]
    public static Matrix4x4<TNum> Lerp(Matrix4x4<TNum> from, Matrix4x4<TNum> to, TNum t)
        => FromRows(
            Vec4<TNum>.Lerp(from.X, to.X, t),
            Vec4<TNum>.Lerp(from.Y, to.Y, t),
            Vec4<TNum>.Lerp(from.Z, to.Z, t),
            Vec4<TNum>.Lerp(from.W, to.W, t)
        );

    [Pure]
    public static Matrix4x4<TNum> CreateShear(
        TNum xy, TNum xz, TNum yx, TNum yz, TNum zx, TNum zy)
        => new(
            TNum.One, xy, xz, TNum.Zero,
            yx, TNum.One, yz, TNum.Zero,
            zx, zy, TNum.One, TNum.Zero,
            TNum.Zero, TNum.Zero, TNum.Zero, TNum.One);

    [Pure]
    public static Matrix4x4<TNum> CreateRotation(Vec3<TNum> axis, TNum angle)
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
        x += new Vec3<TNum>(cos, -sinZ, sinY);
        y += new Vec3<TNum>(sinZ, cos, -sinX);
        z += new Vec3<TNum>(-sinY, sinX, cos);
        return FromRows(x, y, z, Vec4<TNum>.UnitW);
    }

    public static Matrix4x4<TNum> CreateTranslation(Vec3<TNum> translation)
        => Identity + FromRows(Vec4<TNum>.Zero,
            Vec4<TNum>.Zero,
            Vec4<TNum>.Zero,
            translation);

    public static Matrix4x4<TNum> CreateScale(TNum scalar)
        => new(scalar, TNum.Zero, TNum.Zero, TNum.Zero,
            TNum.Zero, scalar, TNum.Zero, TNum.Zero,
            TNum.Zero, TNum.Zero, scalar, TNum.Zero,
            TNum.Zero, TNum.Zero, TNum.Zero, TNum.One);

    public static Matrix4x4<TNum> CreateScale(Vec3<TNum> scalar)
        => new(scalar.X, TNum.Zero, TNum.Zero, TNum.Zero,
            TNum.Zero, scalar.Y, TNum.Zero, TNum.Zero,
            TNum.Zero, TNum.Zero, scalar.Z, TNum.Zero,
            TNum.Zero, TNum.Zero, TNum.Zero, TNum.One);

    public static Matrix4x4<TNum> CreateViewAt(Vec3<TNum> eye, Vec3<TNum> target, Vec3<TNum> up)
    {
        var z = (eye - target).Normalized();
        var x = Vec3<TNum>.Cross(up , z).Normalized();
        var y = Vec3<TNum>.Cross(z , x).Normalized();
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


    public static Matrix4x4<TNum> FromTopLeft(Matrix3x3<TNum> topleft)
        => new(new Vec4<TNum>(topleft.X), new Vec4<TNum>(topleft.Y), new Vec4<TNum>(topleft.Z),
            new Vec4<TNum>(Vec3<TNum>.Zero, TNum.Zero));

    public static Matrix4x4<TNum> FromRows(Vec4<TNum> x, Vec4<TNum> y, Vec4<TNum> z, Vec4<TNum> w)
        => new(x, y, z, w);

    public static Matrix4x4<TNum> FromColumns(Vec4<TNum> x, Vec4<TNum> y, Vec4<TNum> z, Vec4<TNum> w)
        => new(x.X, y.X, z.X, w.X,
            x.Y, y.Y, z.Y, w.Y,
            x.Z, y.Z, z.Z, w.Z,
            x.W, y.W, z.W, w.W);

    public override bool Equals(object? obj) => obj is Matrix4x4<TNum> m && this == m;

    public static bool operator ==(Matrix4x4<TNum> a, Matrix4x4<TNum> b) =>
        a.M00 == b.M00 && a.M01 == b.M01 && a.M02 == b.M02 && a.M03 == b.M03 &&
        a.M10 == b.M10 && a.M11 == b.M11 && a.M12 == b.M12 && a.M13 == b.M13 &&
        a.M20 == b.M20 && a.M21 == b.M21 && a.M22 == b.M22 && a.M23 == b.M23 &&
        a.M30 == b.M30 && a.M31 == b.M31 && a.M32 == b.M32 && a.M33 == b.M33;

    public static bool operator !=(Matrix4x4<TNum> a, Matrix4x4<TNum> b) => !(a == b);

    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);


    public static implicit operator Vec4<Vec4<TNum>>(Matrix4x4<TNum> m) =>
        Unsafe.As<Matrix4x4<TNum>, Vec4<Vec4<TNum>>>(ref m);

    public static implicit operator Matrix4x4<TNum>(Vec4<Vec4<TNum>> n) =>
        Unsafe.As<Vec4<Vec4<TNum>>, Matrix4x4<TNum>>(ref n);

    private Vec4<Vec4<TNum>> Nested()
        => this;

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


    public bool Equals(Matrix4x4<TNum> other)
        => M00 == other.M00 &&
           M01 == other.M01 &&
           M02 == other.M02 &&
           M03 == other.M03 &&
           M10 == other.M10 &&
           M11 == other.M11 &&
           M12 == other.M12 &&
           M13 == other.M13 &&
           M20 == other.M20 &&
           M21 == other.M21 &&
           M22 == other.M22 &&
           M23 == other.M23 &&
           M30 == other.M30 &&
           M31 == other.M31 &&
           M32 == other.M32 &&
           M33 == other.M33;

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
    public static Matrix4x4<TNum> Parse(string s, IFormatProvider? provider)
        => Vec4<Vec4<TNum>>.Parse(s, provider);

    /// <inheritdoc />
    public static Matrix4x4<TNum> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        Vec4<Vec4<TNum>>.Parse(s, provider);

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Matrix4x4<TNum> result)
    {
        result = default;
        return s is { Length: > 0 } && TryParse(s, provider, out result);
    }

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Matrix4x4<TNum> result)
    {
        var success = Vec4<Vec4<TNum>>.TryParse(s, provider, out var n);
        result = n;
        return success;
    }
}