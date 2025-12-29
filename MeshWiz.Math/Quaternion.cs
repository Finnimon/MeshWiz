using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Quaternion<TNum> : IEquatable<Quaternion<TNum>>,
    IEqualityOperators<Quaternion<TNum>, Quaternion<TNum>, bool>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public static Quaternion<TNum> Identity => Vec4<TNum>.UnitW;
    public readonly TNum X, Y, Z, W;
    public Vec3<TNum> Xyz => new(X, Y, Z);

    public Quaternion(Vec3<TNum> xyz, TNum w)
    {
        (X, Y, Z) = xyz;
        W = w;
    }

    public Quaternion(TNum x, TNum y, TNum z, TNum w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public static Quaternion<TNum> FromAngles(TNum rotationX, TNum rotationY, TNum rotationZ)
    {
        rotationX *= Numbers<TNum>.Half;
        rotationY *= Numbers<TNum>.Half;
        rotationZ *= Numbers<TNum>.Half;
        var num1 = TNum.Cos(rotationX);
        var num2 = TNum.Cos(rotationY);
        var num3 = TNum.Cos(rotationZ);
        var num4 = TNum.Sin(rotationX);
        var num5 = TNum.Sin(rotationY);
        var num6 = TNum.Sin(rotationZ);
        var w = num1 * num2 * num3 - num4 * num5 * num6;
        var x = num4 * num2 * num3 + num1 * num5 * num6;
        var y = num1 * num5 * num3 - num4 * num2 * num6;
        var z = num1 * num2 * num6 + num4 * num5 * num3;
        return new Quaternion<TNum>(x, y, z, w);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion<TNum> CreateFromAxisAngle(Vec3<TNum> axis, TNum angle)
    {
        var (sin, num) = TNum.SinCos(angle * Numbers<TNum>.Half);
        return new Vec4<TNum>(axis, TNum.One) * new Vec4<TNum>(Vec3<TNum>.FromValue(sin), num);
    }
    public (Vec3<TNum> Y, Vec3<TNum> Z) Yz()
    {
        var num1 = X * X;
        var num2 = Y * Y;
        var num3 = Z * Z;
        var num4 = X * Y;
        var num5 = Z * W;
        var num6 = Z * X;
        var num7 = Y * W;
        var num8 = Y * Z;
        var num9 = X * W;
        var two = Numbers<TNum>.Two;
        var one = TNum.One;
        var y = Vec3<TNum>.Create((two * (num4 - num5)), (one - two * (num3 + num1)),
            (two * (num8 + num9)));
        var z = Vec3<TNum>.Create((two * (num6 + num7)), (two * (num8 - num9)),
            (one - two * (num2 + num1)));
        return (y, z);
    }
    public static Quaternion<TNum> Slerp(Quaternion<TNum> a, Quaternion<TNum> b, TNum t)
    {
        Vec4<TNum> v1 = a;
        Vec4<TNum> v2 = b;
        var x1 = Vec4<TNum>.Dot(v1, v2);
        var num1 = TNum.One;
        if (x1 < TNum.Zero)
        {
            x1 = -x1;
            num1 = TNum.NegativeOne;
        }

        TNum num2;
        TNum num3;
        if (x1.IsApproxGreaterOrEqual(TNum.One))
        {
            num2 = TNum.One - t;
            num3 = t * num1;
        }
        else
        {
            var x2 = TNum.Acos(x1);
            var num4 = TNum.One / TNum.Sin(x2);
            num2 = TNum.Sin((TNum.One - t) * x2) * num4;
            num3 = TNum.Sin(t * x2) * num4 * num1;
        }

        return v1 * num2 + v2 * num3;
    }

    [Pure]
    public Quaternion<TNum> Normalized() => Vec4<TNum>.Normalize(this);

    /// <inheritdoc />
    public bool Equals(Quaternion<TNum> other) => Xyz == other.Xyz && W == other.W;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Quaternion<TNum> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Xyz.X, Xyz.Y, Xyz.Z, W);

    /// <inheritdoc />
    public static bool operator ==(Quaternion<TNum> left, Quaternion<TNum> right) => left.Equals(right);

    /// <inheritdoc />
    public static bool operator !=(Quaternion<TNum> left, Quaternion<TNum> right)
        => left.Xyz != right.Xyz || left.W != right.W;

    [Pure]
    public static implicit operator Vec4<TNum>(in Quaternion<TNum> q) =>
        Unsafe.As<Quaternion<TNum>, Vec4<TNum>>(ref Unsafe.AsRef(in q));

    [Pure]
    public static implicit operator Quaternion<TNum>(in Vec4<TNum> q) =>
        Unsafe.As<Vec4<TNum>, Quaternion<TNum>>(ref Unsafe.AsRef(in q));

    public Vec3<TNum> Rotate(Vec3<TNum> dir) => AsMatrix3x3() * dir;

    // ReSharper disable once InconsistentNaming
    [Pure]
    public Matrix4x4<TNum> AsMatrix4x4()
    {
        var num1 = X * X;
        var num2 = Y * Y;
        var num3 = Z * Z;
        var num4 = X * Y;
        var num5 = Z * W;
        var num6 = Z * X;
        var num7 = Y * W;
        var num8 = Y * Z;
        var num9 = X * W;
        var two = Numbers<TNum>.Two;
        var one = TNum.One;
        var x = Vec4<TNum>.Create((one - two * (num2 + num3)), (two * (num4 + num5)),
            (two * (num6 - num7)), TNum.Zero);
        var y = Vec4<TNum>.Create((two * (num4 - num5)), (one - two * (num3 + num1)),
            (two * (num8 + num9)), TNum.Zero);
        var z = Vec4<TNum>.Create((two * (num6 + num7)), (two * (num8 - num9)),
            (one - two * (num2 + num1)), TNum.Zero);
        var w = Vec4<TNum>.UnitW;
        return new Matrix4x4<TNum>(x, y, z, w);
    }

    // ReSharper disable once InconsistentNaming
    [Pure]
    public Matrix3x3<TNum> AsMatrix3x3()
    {
        var num1 = X * X;
        var num2 = Y * Y;
        var num3 = Z * Z;
        var num4 = X * Y;
        var num5 = Z * W;
        var num6 = Z * X;
        var num7 = Y * W;
        var num8 = Y * Z;
        var num9 = X * W;
        var two = Numbers<TNum>.Two;
        var one = TNum.One;
        var x = Vec3<TNum>.Create((one - two * (num2 + num3)), (two * (num4 + num5)),
            (two * (num6 - num7)));
        var y = Vec3<TNum>.Create((two * (num4 - num5)), (one - two * (num3 + num1)),
            (two * (num8 + num9)));
        var z = Vec3<TNum>.Create((two * (num6 + num7)), (two * (num8 - num9)),
            (one - two * (num2 + num1)));
        return new Matrix3x3<TNum>(x, y, z);
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3<TNum> UnitX()
    {
        var x = TNum.One - Numbers<TNum>.Two * (Y*Y + Z*Z);
        var y = Numbers<TNum>.Two * (X*Y + Z*W);
        var z = Numbers<TNum>.Two * (Z*X - Y*W);
        return Vec3<TNum>.Create(x, y, z);
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3<TNum> UnitY()
    {
        var x = Numbers<TNum>.Two * (X*Y - Z*W);
        var y = TNum.One - Numbers<TNum>.Two * (Z*Z + X*X);
        var z = Numbers<TNum>.Two * (Y*Z + X*W);
        return Vec3<TNum>.Create(x, y, z);
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3<TNum> UnitZ()
    {
        var x = Numbers<TNum>.Two * (Z * X + Y * W);
        var y = Numbers<TNum>.Two * (Y * Z - X * W);
        var z = TNum.One - Numbers<TNum>.Two * (Y * Y + X * X);
        return Vec3<TNum>.Create(x, y, z);
    }
    
    [Pure]
    internal static Quaternion<TNum> CreateUnsafe(in Matrix3x3<TNum> matrix)
    {
        var num1 = matrix.M00 + matrix.M11 + matrix.M22;
        TNum x, y, z, w;
        if (num1 > TNum.Zero)
        {
            var num2 = TNum.Sqrt(num1 + TNum.One);
            w = num2 * Numbers<TNum>.Half;
            var num3 = Numbers<TNum>.Half / num2;
            x = (matrix.M12 - matrix.M21) * num3;
            y = (matrix.M20 - matrix.M02) * num3;
            z = (matrix.M01 - matrix.M10) * num3;
        }
        else if (matrix.M00 >= matrix.M11 && matrix.M00 >= matrix.M22)
        {
            var num4 = TNum.Sqrt(TNum.One + matrix.M00 - matrix.M11 - matrix.M22);
            var num5 = Numbers<TNum>.Half / num4;
            x = Numbers<TNum>.Half * num4;
            y = (matrix.M01 + matrix.M10) * num5;
            z = (matrix.M02 + matrix.M20) * num5;
            w = (matrix.M12 - matrix.M21) * num5;
        }
        else if (matrix.M11 > matrix.M22)
        {
            var num6 = TNum.Sqrt(TNum.One + matrix.M11 - matrix.M00 - matrix.M22);
            var num7 = Numbers<TNum>.Half / num6;
            x = (matrix.M10 + matrix.M01) * num7;
            y = Numbers<TNum>.Half * num6;
            z = (matrix.M21 + matrix.M12) * num7;
            w = (matrix.M20 - matrix.M02) * num7;
        }
        else
        {
            var num8 = TNum.Sqrt(TNum.One + matrix.M22 - matrix.M00 - matrix.M11);
            var num9 = Numbers<TNum>.Half / num8;
            x = (matrix.M20 + matrix.M02) * num9;
            y = (matrix.M21 + matrix.M12) * num9;
            z = Numbers<TNum>.Half * num8;
            w = (matrix.M01 - matrix.M10) * num9;
        }

        return new Quaternion<TNum>(x, y, z, w);
    }

    public Quaternion<TOtherNum> To<TOtherNum>() where TOtherNum : unmanaged, IFloatingPointIeee754<TOtherNum>
    {
        Vec4<TNum> vec4 = this;
        return vec4.To<TOtherNum>();
    }
}