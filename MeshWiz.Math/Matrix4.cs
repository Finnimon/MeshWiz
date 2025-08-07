using System;
using System.Numerics;
using System.Runtime.InteropServices;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Matrix4<TNum> : IMatrix<TNum>, IEquatable<Matrix4<TNum>>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public static int RowCount => 4;
    public static int ColumnCount => 4;

    public static Matrix4<TNum> Identity => new(
        TNum.One, TNum.Zero, TNum.Zero, TNum.Zero,
        TNum.Zero, TNum.One, TNum.Zero, TNum.Zero,
        TNum.Zero, TNum.Zero, TNum.One, TNum.Zero,
        TNum.Zero, TNum.Zero, TNum.Zero, TNum.One);

    public static Matrix4<TNum> Zero => new(TNum.Zero);
    public static Matrix4<TNum> One => new(TNum.One);
    public TNum Det => Determinant();

    public unsafe Vector4<TNum> X
    {
        get
        {
            fixed (TNum* ptr = &M00) return *(Vector4<TNum>*)ptr;
        }
    }

    public unsafe Vector4<TNum> Y
    {
        get
        {
            fixed (TNum* ptr = &M10) return *(Vector4<TNum>*)ptr;
        }
    }

    public unsafe Vector4<TNum> Z
    {
        get
        {
            fixed (TNum* ptr = &M20) return *(Vector4<TNum>*)ptr;
        }
    }

    public unsafe Vector4<TNum> W
    {
        get
        {
            fixed (TNum* ptr = &M30) return *(Vector4<TNum>*)ptr;
        }
    }

    public Vector4<TNum> Diagonal => new Vector4<TNum>(M00, M11, M22, M33);
    public TNum Trace => M00 + M11 + M22 + M33;
    public Matrix4<TNum> Normlized => this / Det;

    public readonly TNum
        M00,
        M01,
        M02,
        M03,
        M10,
        M11,
        M12,
        M13,
        M20,
        M21,
        M22,
        M23,
        M30,
        M31,
        M32,
        M33;

    public Matrix4(
        TNum m00, TNum m01, TNum m02, TNum m03,
        TNum m10, TNum m11, TNum m12, TNum m13,
        TNum m20, TNum m21, TNum m22, TNum m23,
        TNum m30, TNum m31, TNum m32, TNum m33)
    {
        M00 = m00;
        M01 = m01;
        M02 = m02;
        M03 = m03;
        M10 = m10;
        M11 = m11;
        M12 = m12;
        M13 = m13;
        M20 = m20;
        M21 = m21;
        M22 = m22;
        M23 = m23;
        M30 = m30;
        M31 = m31;
        M32 = m32;
        M33 = m33;
    }

    public Matrix4(TNum value)
    {
        M00 = value;
        M01 = value;
        M02 = value;
        M03 = value;
        M10 = value;
        M11 = value;
        M12 = value;
        M13 = value;
        M20 = value;
        M21 = value;
        M22 = value;
        M23 = value;
        M30 = value;
        M31 = value;
        M32 = value;
        M33 = value;
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
        M00 = x.X;
        M01 = x.Y;
        M02 = x.Z;
        M03 = x.W;
        M10 = y.X;
        M11 = y.Y;
        M12 = y.Z;
        M13 = y.W;
        M20 = z.X;
        M21 = z.Y;
        M22 = z.Z;
        M23 = z.W;
        M30 = w.X;
        M31 = w.Y;
        M32 = w.Z;
        M33 = w.W;
    }


    public unsafe TNum this[int row, int column]
    {
        get
        {
            if (row.InsideInclusiveRange(0, 3) && column.InsideInclusiveRange(0, 3))
                fixed (TNum* ptr = &M00)
                    return ptr[row * ColumnCount + column];
            throw new ArgumentOutOfRangeException();
        }
    }

    public unsafe TNum[,] ToArrayFast()
    {
        var result = new TNum[4, 4];
        fixed (TNum* sourcePtr = &M00)
        fixed (TNum* destPtr = &result[0, 0])
        {
            var bytes = (long)4 * 4 * sizeof(TNum);
            Buffer.MemoryCopy(sourcePtr, destPtr, bytes, bytes);
        }

        return result;
    }

    public Matrix4<TNum> Transpose() => FromColumns(X, Y, Z, W);

    public static Matrix4<TNum> Transpose(Matrix4<TNum> m) => m.Transpose();

    public static Matrix4<TNum> operator *(Matrix4<TNum> a, Matrix4<TNum> b)
    {
        var aX = a.X;
        var aY = a.Y;
        var aZ = a.Z;
        var aW = a.W;
        var b0 = b.GetColumn(0);
        var b1 = b.GetColumn(1);
        var b2 = b.GetColumn(2);
        var b3 = b.GetColumn(3);
        
        return new Matrix4<TNum>(
            aX*b0,aX*b1,aX*b2,aX*b3,
            aY*b0,aY*b1,aY*b2,aY*b3,
            aZ*b0,aZ*b1,aZ*b2,aZ*b3,
            aW*b0,aW*b1,aW*b2,aW*b3
            );
    }
        // => new(
        //     a.M00 * b.M00 + a.M01 * b.M10 + a.M02 * b.M20 + a.M03 * b.M30,
        //     a.M00 * b.M01 + a.M01 * b.M11 + a.M02 * b.M21 + a.M03 * b.M31,
        //     a.M00 * b.M02 + a.M01 * b.M12 + a.M02 * b.M22 + a.M03 * b.M32,
        //     a.M00 * b.M03 + a.M01 * b.M13 + a.M02 * b.M23 + a.M03 * b.M33,
        //     a.M10 * b.M00 + a.M11 * b.M10 + a.M12 * b.M20 + a.M13 * b.M30,
        //     a.M10 * b.M01 + a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31,
        //     a.M10 * b.M02 + a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32,
        //     a.M10 * b.M03 + a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33,
        //     a.M20 * b.M00 + a.M21 * b.M10 + a.M22 * b.M20 + a.M23 * b.M30,
        //     a.M20 * b.M01 + a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31,
        //     a.M20 * b.M02 + a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32,
        //     a.M20 * b.M03 + a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33,
        //     a.M30 * b.M00 + a.M31 * b.M10 + a.M32 * b.M20 + a.M33 * b.M30,
        //     a.M30 * b.M01 + a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31,
        //     a.M30 * b.M02 + a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32,
        //     a.M30 * b.M03 + a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33);

        
    public Vector3<TNum> MultiplyPoint(Vector3<TNum> v)
    {
        var v4 = new Vector4<TNum>(v,TNum.One);
        var x = X*v4;
        var y = Y*v4;
        var z = Y*v4;
        var w = W*v4;
        return new Vector3<TNum>(x , y, z)/w;
    }

    public Vector3<TNum> MultiplyDirection(Vector3<TNum> v)
        => new(X.XYZ*v, Y.XYZ*v, Z.XYZ*v);

    public Vector4<TNum> Multiply(Vector4<TNum> v)
        => new(
            M00 * v.X + M01 * v.Y + M02 * v.Z + M03 * v.W,
            M10 * v.X + M11 * v.Y + M12 * v.Z + M13 * v.W,
            M20 * v.X + M21 * v.Y + M22 * v.Z + M23 * v.W,
            M30 * v.X + M31 * v.Y + M32 * v.Z + M33 * v.W);

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
        if (row.OutsideInclusiveRange(0, 3)) throw new ArgumentOutOfRangeException(nameof(row));
        fixed (TNum* ptr = &M00) return *(Vector4<TNum>*)(ptr + row * ColumnCount);
    }

    public Vector4<TNum> GetColumn(int col)
    {
        return col switch
        {
            0 => new Vector4<TNum>(M00, M10, M20, M30),
            1 => new Vector4<TNum>(M01, M11, M21, M31),
            2 => new Vector4<TNum>(M02, M12, M22, M32),
            3 => new Vector4<TNum>(M03, M13, M23, M33),
            _ => throw new ArgumentOutOfRangeException(nameof(col))
        };
    }

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
        var x = Vector3<TNum>.ElementWiseMul(axis.XXX, tAxis);
        var y = Vector3<TNum>.ElementWiseMul(axis.YYY, tAxis);
        var z = Vector3<TNum>.ElementWiseMul(axis.ZZZ, tAxis);
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
    =>new(scalar,TNum.Zero,TNum.Zero,TNum.Zero,
        TNum.Zero,scalar,TNum.Zero,TNum.Zero,
        TNum.Zero,TNum.Zero,scalar,TNum.Zero,
        TNum.Zero,TNum.Zero,TNum.Zero,TNum.One);

    public static Matrix4<TNum> CreateScale(Vector3<TNum> scalar)
    =>new(scalar.X,TNum.Zero,TNum.Zero,TNum.Zero,
        TNum.Zero,scalar.Y,TNum.Zero,TNum.Zero,
        TNum.Zero,TNum.Zero,scalar.Z,TNum.Zero,
        TNum.Zero,TNum.Zero,TNum.Zero,TNum.One);
    
    public static Matrix4<TNum> CreateViewAt(Vector3<TNum> eye, Vector3<TNum> target, Vector3<TNum> up)
    {
        var z = (eye - target).Normalized;
        var x=(up^z).Normalized;
        var y=(z^x).Normalized;
        var w = Vector4<TNum>.FromXYZW(
            -(x*eye),
            -(y*eye),
            -(z*eye),
            TNum.One
        );
        return FromRows(new Vector4<TNum>(x.X,y.X,z.X,TNum.Zero), 
            new Vector4<TNum>(x.Y,y.Y,z.Y,TNum.Zero), 
            new Vector4<TNum>(x.Z,y.Z,z.Z,TNum.Zero), 
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