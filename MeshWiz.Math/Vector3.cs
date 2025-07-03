using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.InteropServices;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Vector3<TNum> : IVector3<Vector3<TNum>, TNum>
where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum>
{
    #region Fields
    public readonly TNum X, Y, Z;
    #endregion
    #region computed props
    public static unsafe int ByteSize => sizeof(TNum)*3;
    public int Count => 3;
    static uint IVector<Vector3<TNum>, TNum>.Dimensions => 3;
    public Vector3<TNum> Normalized => this/Length;

    [Pure] public uint Dimensions => 3;
    [Pure] public TNum Length => TNum.Sqrt(SquaredLength);

    [Pure]
    public TNum SquaredLength
        => X * X + Y * Y + Z * Z;

    public TNum AlignedCuboidVolume => TNum.Abs(X * Y * Z);

    #endregion
    #region ctor
    public Vector3(TNum x , TNum y , TNum z)
    {
        X = x;
        Y = y;
        Z = z;
    }
    public static Vector3<TNum> FromXYZ(TNum x, TNum y, TNum z)
        => new(x, y, z);
    public Vector3<TNum> ZYX=>new(Z,Y,Y);
    public Vector3<TNum> YZX=>new(Y,Z,Y);
    public Vector3<TNum> YXZ=>new(Y,Y,Z);
    public Vector3<TNum> XZY=>new(Y,Z,Y);
    public Vector3<TNum> ZXY=>new(Z,Y,Y);
    public static Vector3<TNum> Zero => new(TNum.Zero, TNum.Zero, TNum.Zero);
    public static Vector3<TNum> One => new(TNum.One, TNum.One, TNum.One);
    public static Vector3<TNum> NaN => new(TNum.NaN, TNum.NaN, TNum.NaN);
    #endregion



    #region arithmetic

    #region operators

    [Pure]
    public static Vector3<TNum> operator +(in Vector3<TNum> left, in Vector3<TNum> right)
        => new (left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    [Pure]
    public static Vector3<TNum> operator -(in Vector3<TNum> left, in Vector3<TNum> right)
        => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    [Pure]
    public static TNum operator *(in Vector3<TNum> left, in Vector3<TNum> right)
        => left.X * right.X + left.Y * right.Y + left.Z * right.Z;

    [Pure]
    public static Vector3<TNum> operator *(in Vector3<TNum> vec, TNum scalar)
        => new(x: vec.X * scalar, y: vec.Y * scalar, z: vec.Z * scalar);

    [Pure]
    public static Vector3<TNum> operator *(TNum scalar, in Vector3<TNum> vec)
        => new(vec.X * scalar, vec.Y * scalar, vec.Z * scalar);

    [Pure]
    public static Vector3<TNum> operator /(in Vector3<TNum> vec, TNum divisor)
        => vec * (TNum.One / divisor);


    [Pure]
    public static Vector3<TNum> operator ^(in Vector3<TNum> left, in Vector3<TNum> right)
        => new(
            x: left.Y * right.Z - left.Z * right.Y,
            y: left.Z * right.X - left.X * right.Z,
            z: left.X * right.Y - left.Y * right.X
        );

    [Pure,SuppressMessage("ReSharper", "CompareOfTNumsByEqualityOperator")]
    public static bool operator ==(in Vector3<TNum> left, in Vector3<TNum> right)
        => left.X == right.X && left.Y == right.Y && left.Z == right.Z;

    [Pure,SuppressMessage("ReSharper", "CompareOfTNumsByEqualityOperator")]
    public static bool operator !=(in Vector3<TNum> left, in Vector3<TNum> right)
        => left.X != right.X || left.Y != right.Y || left.Z != right.Z;

    #endregion

    #region functions

    [Pure]
    public Vector3<TNum> Add(in Vector3<TNum> other)
        => this + other;

    [Pure]
    public Vector3<TNum> Subtract(in Vector3<TNum> other)
        => this - other;

    [Pure]
    public Vector3<TNum> Scale(in TNum scalar)
        => this * scalar;

    [Pure]
    public Vector3<TNum> Divide(in TNum divisor)
        => this / divisor;

    
    [Pure]
    public TNum Dot(in Vector3<TNum> other) => this * other;

    [Pure]
    public TNum Distance(in Vector3<TNum> other) => (this - other).Length;

    [Pure]
    public Vector3<TNum> Cross(in Vector3<TNum> other) => this ^ other;
    
    #endregion

    #endregion

    #region functions

    #region general

    [Pure]
    public bool Equals(Vector3<TNum> other)
        => this == other;

    [Pure]
    public int CompareTo(Vector3<TNum> other)
        => SquaredLength.CompareTo(other.SquaredLength);

    [Pure]
    public unsafe ReadOnlySpan<TNum> AsSpan()
    {
        fixed (TNum* ptr = &X) return new ReadOnlySpan<TNum>(ptr, 3);
    }

    [Pure]
    public override bool Equals(object? other)
        => other is Vector3<TNum> vec && vec == this;

    [Pure]
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    #endregion

    #endregion

    #region IReadOnlyList

    [Pure]
    public unsafe TNum this[int index]
    {
        [Pure]
        get
        {
            if (index.InsideInclusiveRange(0, 2))
                fixed (TNum* ptr = &X)
                    return ptr[index];
            throw new IndexOutOfRangeException();
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

    public void Deconstruct(out TNum x, out TNum y, out TNum z)
    {
        x = X;
        y = Y;
        z = Z;
    }

    #endregion
    
    public static Vector3<TNum> Lerp(in Vector3<TNum> from, in Vector3<TNum> to, TNum normalDistance)
        =>(to-from)*normalDistance+from;
    
    
    [SuppressMessage("ReSharper", "UseStringInterpolation")]
    public override string ToString()
        => string.Format("{{X:{0:F3} Y:{1:F3} Z:{2:F3}}}", X, Y, Z);
}