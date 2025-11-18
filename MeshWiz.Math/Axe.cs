using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public readonly struct Quaternion<TNum> : IEquatable<Quaternion<TNum>>,
    IEqualityOperators<Quaternion<TNum>, Quaternion<TNum>, bool>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public static Quaternion<TNum> Identity => Vector4<TNum>.UnitW;
    public readonly TNum X, Y, Z, W;
    public Vector3<TNum> Xyz => new(X, Y, Z);

    public Quaternion(Vector3<TNum> xyz, TNum w)
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
    public static Quaternion<TNum> CreateFromAxisAngle(Vector3<TNum> axis, TNum angle)
    {
        var (sin, num) = TNum.SinCos(angle * Numbers<TNum>.Half);
        return new Vector4<TNum>(axis, TNum.One) * new Vector4<TNum>(Vector3<TNum>.FromValue(sin), num);
    }

    public static Quaternion<TNum> Slerp(Quaternion<TNum> a, Quaternion<TNum> b, TNum t)
    {
        Vector4<TNum> v1 = a;
        Vector4<TNum> v2 = b;
        var x1 = Vector4<TNum>.Dot(v1,v2);
        var num1 = TNum.One;
        if ( x1 < TNum.Zero)
        {
            x1 = -x1;
            num1 = TNum.NegativeOne;
        }
        TNum num2;
        TNum num3;
        if ( x1.IsApproxGreaterOrEqual(TNum.One))
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
    public Quaternion<TNum> Normalized() => Vector4<TNum>.Normalize(this);
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
    public static implicit operator Vector4<TNum>(in Quaternion<TNum> q) =>
        Unsafe.As<Quaternion<TNum>, Vector4<TNum>>(ref Unsafe.AsRef(in q));

    [Pure]
    public static implicit operator Quaternion<TNum>(in Vector4<TNum> q) =>
        Unsafe.As<Vector4<TNum>, Quaternion<TNum>>(ref Unsafe.AsRef(in q));

    public Vector3<TNum> Rotate(Vector3<TNum> dir)
    {
        throw new NotImplementedException();
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct Pose3<TNum> : IPose<Pose3<TNum>, Vector3<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Quaternion<TNum> Rotation;
    public readonly Vector3<TNum> Origin;
    public Vector3<TNum> Position => Origin;

    public Pose3()
    {
        Origin = Vector3<TNum>.Zero;
        Rotation = new();
    }

    public Pose3(Quaternion<TNum> rotation, Vector3<TNum> origin)
    {
        Rotation = rotation;
        Origin = origin;
    }


    /// <inheritdoc />
    public TNum DistanceTo(Pose3<TNum> other) => Origin.DistanceTo(other.Origin);

    /// <inheritdoc />
    public TNum SquaredDistanceTo(Pose3<TNum> other)
        => Origin.SquaredDistanceTo(other.Origin);

    /// <inheritdoc />
    public static TNum Distance(Pose3<TNum> a, Pose3<TNum> b) => Vector3<TNum>.Distance(a.Origin, b.Origin);

    /// <inheritdoc />
    public static TNum SquaredDistance(Pose3<TNum> a, Pose3<TNum> b) =>
        Vector3<TNum>.SquaredDistance(a.Origin, b.Origin);


    /// <inheritdoc />
    public static Pose3<TNum> Lerp(Pose3<TNum> a, Pose3<TNum> b, TNum t)
        => new(Quaternion<TNum>.Slerp(a.Rotation, b.Rotation, t), Vector3<TNum>.Lerp(a.Origin, b.Origin, t));

    /// <inheritdoc />
    public static Pose3<TNum> ExactLerp(Pose3<TNum> a, Pose3<TNum> b, TNum exactDistance)
    {
        var distance = a.DistanceTo(b);
        return Lerp(a, b, exactDistance / distance);
    }

    /// <inheritdoc />
    public Vector3<TNum> Transform(Vector3<TNum> src)
    {
        var translated = src - Origin;
        return Rotation.Rotate(translated);
    }
}

public interface IPose<TSelf, TVector, TNum>
    : IPosition<TSelf, TVector, TNum>,
        ILerp<TSelf, TNum>,
        ITransform<TVector>
    where TSelf : IPosition<TSelf, TVector, TNum>, ILerp<TSelf, TNum>
    where TVector : unmanaged, IVector<TVector, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum> { }