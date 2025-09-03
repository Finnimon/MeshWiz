using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Matrix4<TNum> : IMatrix<TNum>, IEquatable<Matrix4<TNum>>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public static int RowCount => 4;
    public static int ColumnCount => 4;


    [Pure]
    public static Matrix4<TNum> Identity
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } =
        new(Vector4<TNum>.UnitX,
            Vector4<TNum>.UnitY,
            Vector4<TNum>.UnitZ,
            Vector4<TNum>.UnitW);


    public static Matrix4<TNum> Zero
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = new(TNum.Zero);

    public static Matrix4<TNum> One
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = new(TNum.One);

    public static Matrix4<TNum> NaN
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = new(TNum.NaN);

    public TNum Det => Determinant();

    public Vector4<TNum> Diagonal => new(M00, M11, M22, M33);
    public TNum Trace => M00 + M11 + M22 + M33;
    public Matrix4<TNum> Normlized => this / Det;

    public readonly Vector4<TNum> X, Y, Z, W;

// @formatter:off
    public TNum M00 => X.X; public TNum M01 => X.Y; public TNum M02 => X.Z; public TNum M03 => X.W;
    public TNum M10 => Y.X; public TNum M11 => Y.Y; public TNum M12 => Y.Z; public TNum M13 => Y.W;
    public TNum M20 => Z.X; public TNum M21 => Z.Y; public TNum M22 => Z.Z; public TNum M23 => Z.W;
    public TNum M30 => W.X; public TNum M31 => W.Y; public TNum M32 => W.Z; public TNum M33 => W.W;
// @formatter:on


    public Matrix4(
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

    public Matrix4(TNum value)
    {
        X = new(value);
        Y = X;
        Z = Y;
        W = Z;
    }

    public unsafe Matrix4(TNum[] components)
    {
        if (components.Length != 16)
            throw new ArgumentException("Components must be of length 16");
        fixed (TNum* ptr = &components[0])
            this = *(Matrix4<TNum>*)ptr;
    }

    public Matrix4(Vector4<TNum> x, Vector4<TNum> y, Vector4<TNum> z, Vector4<TNum> w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }


    public unsafe TNum this[int row, int column]
    {
        get
        {
            if (RowCount > (uint)row && RowCount > (uint)column)
                fixed (TNum* ptr = &X.X)
                    return ptr[row * ColumnCount + column];
            throw new ArgumentOutOfRangeException();
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

    public static unsafe ReadOnlySpan<TNum> AsSpan(in Matrix4<TNum> matrix)
    {
        fixed (void* sourcePtr = &matrix) return new(sourcePtr, RowCount * ColumnCount);
    }

    public Matrix4<TNum> Transpose() => FromColumns(X, Y, Z, W);

    public static Matrix4<TNum> Transpose(Matrix4<TNum> m) => m.Transpose();

    public static Matrix4<TNum> operator *(Matrix4<TNum> a, Matrix4<TNum> b)
    {
        b = b.Transpose();
        return new Matrix4<TNum>(
            a.X.Dot( b.X), a.X.Dot(b.Y), a.X.Dot(b.Z), a.X.Dot(b.W),
            a.Y.Dot( b.X), a.Y.Dot(b.Y), a.Y.Dot(b.Z), a.Y.Dot(b.W),
            a.Z.Dot( b.X), a.Z.Dot(b.Y), a.Z.Dot(b.Z), a.Z.Dot(b.W),
            a.W.Dot( b.X), a.W.Dot(b.Y), a.W.Dot(b.Z), a.W.Dot(b.W)
        );
    }


    public Vector3<TNum> MultiplyPoint(Vector3<TNum> v)
    {
        var v4 = new Vector4<TNum>(v, TNum.One);
        v4 = Multiply(v4);
        return v4.XYZ / v4.W;
    }

    public Vector3<TNum> MultiplyDirection(Vector3<TNum> v)
        => new(X.XYZ.Dot(v), Y.XYZ.Dot(v), Z.XYZ.Dot(v));

    public Vector4<TNum> Multiply(Vector4<TNum> v)
        => new(X.Dot(v), Y.Dot(v), Z.Dot(v), W.Dot(v));

    public static Vector4<TNum> operator *(Matrix4<TNum> m, Vector4<TNum> v) => m.Multiply(v);
    public static Vector3<TNum> operator *(Matrix4<TNum> m, Vector3<TNum> v) => m.MultiplyPoint(v);

    public static Matrix4<TNum> operator *(Matrix4<TNum> m, TNum scalar) =>
        new(m.X * scalar, m.Y * scalar, m.Z * scalar, m.W * scalar);

    public static Matrix4<TNum> operator *(TNum scalar, Matrix4<TNum> m) => m * scalar;
    public static Matrix4<TNum> operator /(Matrix4<TNum> m, TNum divisor) => m * (TNum.One / divisor);

    public static Matrix4<TNum> operator +(Matrix4<TNum> l, Matrix4<TNum> r) =>
        new(l.X + r.X, l.Y + r.Y, l.Z + r.Z, l.W + r.W);

    public static Matrix4<TNum> operator -(Matrix4<TNum> m) => new(-m.X, -m.Y, -m.Z, -m.W);

    public static Matrix4<TNum> operator -(Matrix4<TNum> l, Matrix4<TNum> r)
        => new(l.X - r.X, l.Y - r.Y, l.Z - r.Z, l.W - r.W);


    // Row/Column access via bitwise cast
    public unsafe Vector4<TNum> GetRow(int row)
    {
        if (RowCount > (uint)row)
            fixed (Vector4<TNum>* ptr = &X)
                return ptr[row * ColumnCount];
        throw new ArgumentOutOfRangeException(nameof(row));
    }


    public Vector4<TNum> GetColumn(int col) => Transpose().GetRow(col);

    // Homogeneous utilities
    public static Vector3<TNum> Homogenize(Vector4<TNum> v)
        => new(v.X / v.W, v.Y / v.W, v.Z / v.W);

    public static Vector4<TNum> Dehomogenize(Vector3<TNum> v, TNum w)
        => new(v.X, v.Y, v.Z, w);

    // Linear interpolation
    public static Matrix4<TNum> Lerp(Matrix4<TNum> from, Matrix4<TNum> to, TNum t)
        => FromRows(
            Vector4<TNum>.Lerp(from.X, to.X, t),
            Vector4<TNum>.Lerp(from.Y, to.Y, t),
            Vector4<TNum>.Lerp(from.Z, to.Z, t),
            Vector4<TNum>.Lerp(from.W, to.W, t)
        );

    // Shear
    public static Matrix4<TNum> CreateShear(
        TNum xy, TNum xz, TNum yx, TNum yz, TNum zx, TNum zy)
        => new(
            TNum.One, xy, xz, TNum.Zero,
            yx, TNum.One, yz, TNum.Zero,
            zx, zy, TNum.One, TNum.Zero,
            TNum.Zero, TNum.Zero, TNum.Zero, TNum.One);

    public static Matrix4<TNum> CreateRotation(Vector3<TNum> axis, TNum angle)
    {
        axis = axis.Normalized;

        var cos = TNum.Cos(-angle);
        var sin = TNum.Sin(-angle);
        var t = TNum.One - cos;
        var tAxis = t * axis;
        var x = axis.XXX*tAxis;
        var y = axis.YYY*tAxis;
        var z = axis.ZZZ*tAxis;
        var (sinX, sinY, sinZ) = sin * axis;
        x += new Vector3<TNum>(cos, -sinZ, sinY);
        y += new Vector3<TNum>(sinZ, cos, -sinX);
        z += new Vector3<TNum>(-sinY, sinX, cos);
        return FromRows(x, y, z, Vector4<TNum>.UnitW);
    }

    public static Matrix4<TNum> CreateTranslation(Vector3<TNum> translation)
        => Identity + FromRows(Vector4<TNum>.Zero,
            Vector4<TNum>.Zero,
            Vector4<TNum>.Zero,
            translation);

    public static Matrix4<TNum> CreateScale(TNum scalar)
        => new(scalar, TNum.Zero, TNum.Zero, TNum.Zero,
            TNum.Zero, scalar, TNum.Zero, TNum.Zero,
            TNum.Zero, TNum.Zero, scalar, TNum.Zero,
            TNum.Zero, TNum.Zero, TNum.Zero, TNum.One);

    public static Matrix4<TNum> CreateScale(Vector3<TNum> scalar)
        => new(scalar.X, TNum.Zero, TNum.Zero, TNum.Zero,
            TNum.Zero, scalar.Y, TNum.Zero, TNum.Zero,
            TNum.Zero, TNum.Zero, scalar.Z, TNum.Zero,
            TNum.Zero, TNum.Zero, TNum.Zero, TNum.One);

    public static Matrix4<TNum> CreateViewAt(Vector3<TNum> eye, Vector3<TNum> target, Vector3<TNum> up)
    {
        var z = (eye - target).Normalized;
        var x = (up ^ z).Normalized;
        var y = (z ^ x).Normalized;
        var w = Vector4<TNum>.FromXYZW(
            -(x.Dot(eye)),
            -(y.Dot(eye)),
            -(z.Dot(eye)),
            TNum.One
        );
        return FromRows(new Vector4<TNum>(x.X, y.X, z.X, TNum.Zero),
            new Vector4<TNum>(x.Y, y.Y, z.Y, TNum.Zero),
            new Vector4<TNum>(x.Z, y.Z, z.Z, TNum.Zero),
            w);
    }


    public static Matrix4<TNum> FromTopLeft(Matrix3<TNum> topleft)
        => new(new Vector4<TNum>(topleft.X), new Vector4<TNum>(topleft.Y), new Vector4<TNum>(topleft.Z),
            new Vector4<TNum>(Vector3<TNum>.Zero, TNum.Zero));

    public static Matrix4<TNum> FromRows(Vector4<TNum> x, Vector4<TNum> y, Vector4<TNum> z, Vector4<TNum> w)
        => new(x, y, z, w);

    public static Matrix4<TNum> FromColumns(Vector4<TNum> x, Vector4<TNum> y, Vector4<TNum> z, Vector4<TNum> w)
        => new(x.X, y.X, z.X, w.X,
            x.Y, y.Y, z.Y, w.Y,
            x.Z, y.Z, z.Z, w.Z,
            x.W, y.W, z.W, w.W);

    public override bool Equals(object? obj) => obj is Matrix4<TNum> m && this == m;

    public static bool operator ==(Matrix4<TNum> a, Matrix4<TNum> b) =>
        a.M00 == b.M00 && a.M01 == b.M01 && a.M02 == b.M02 && a.M03 == b.M03 &&
        a.M10 == b.M10 && a.M11 == b.M11 && a.M12 == b.M12 && a.M13 == b.M13 &&
        a.M20 == b.M20 && a.M21 == b.M21 && a.M22 == b.M22 && a.M23 == b.M23 &&
        a.M30 == b.M30 && a.M31 == b.M31 && a.M32 == b.M32 && a.M33 == b.M33;

    public static bool operator !=(Matrix4<TNum> a, Matrix4<TNum> b) => !(a == b);

    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);

    public override string ToString() =>
        $"[{M00},{M01},{M02},{M03}; {M10},{M11},{M12},{M13}; {M20},{M21},{M22},{M23}; {M30},{M31},{M32},{M33}]";

    public TNum Determinant()
    {
        // Compute 3x3 minors
        TNum det00 = M11 * (M22 * M33 - M23 * M32) - M12 * (M21 * M33 - M23 * M31) + M13 * (M21 * M32 - M22 * M31);
        TNum det01 = M10 * (M22 * M33 - M23 * M32) - M12 * (M20 * M33 - M23 * M30) + M13 * (M20 * M32 - M22 * M30);
        TNum det02 = M10 * (M21 * M33 - M23 * M31) - M11 * (M20 * M33 - M23 * M30) + M13 * (M20 * M31 - M21 * M30);
        TNum det03 = M10 * (M21 * M32 - M22 * M31) - M11 * (M20 * M32 - M22 * M30) + M12 * (M20 * M31 - M21 * M30);
        return
            M00 * det00
            - M01 * det01
            + M02 * det02
            - M03 * det03;
    }


    public bool Equals(Matrix4<TNum> other)
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
}