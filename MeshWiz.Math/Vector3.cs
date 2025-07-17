using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Vector3<TNum> : IVector3<Vector3<TNum>, TNum>
where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly TNum X, Y, Z;

    public static unsafe int ByteSize => sizeof(TNum)*3;
    public int Count => 3;
    
    [Pure] public Vector3<TNum> Normalized => this/Length;
    [Pure] public static uint Dimensions => 3;
    [Pure] public TNum Length => TNum.Sqrt(SquaredLength);

    [Pure]
    public TNum SquaredLength
        => X * X + Y * Y + Z * Z;

    public TNum AlignedCuboidVolume => TNum.Abs(X * Y * Z);

    public Vector3(TNum x , TNum y , TNum z)
    {
        X = x;
        Y = y;
        Z = z;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> FromXYZ(TNum x, TNum y, TNum z)
        => new(x, y, z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> FromComponents(TNum[] components)=>new(components[0], components[1],components[2]);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> FromComponents(ReadOnlySpan<TNum> components)=>new(components[0], components[1],components[2]);

    public Vector3<TNum> ZYX=>new(Z,Y,Y);
    public Vector3<TNum> YZX=>new(Y,Z,Y);
    public Vector3<TNum> YXZ=>new(Y,Y,Z);
    public Vector3<TNum> XZY=>new(Y,Z,Y);
    public Vector3<TNum> ZXY=>new(Z,Y,Y);
    public static Vector3<TNum> Zero => new(TNum.Zero, TNum.Zero, TNum.Zero);
    public static Vector3<TNum> One => new(TNum.One, TNum.One, TNum.One);
    public static Vector3<TNum> NaN => new(TNum.NaN, TNum.NaN, TNum.NaN);
    public static Vector3<TNum> UnitX=>new(TNum.One,TNum.Zero,TNum.Zero);
    public static Vector3<TNum> UnitY=>new(TNum.Zero,TNum.One,TNum.Zero);
    public static Vector3<TNum> UnitZ=>new(TNum.Zero,TNum.Zero,TNum.One);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<TOther> To<TOther>() where TOther : unmanaged, IFloatingPointIeee754<TOther>
        => new(TOther.CreateTruncating(X), TOther.CreateTruncating(Y),TOther.CreateTruncating(Z));


    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator +(Vector3<TNum> left, Vector3<TNum> right)
        => new (left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator -(Vector3<TNum> left, Vector3<TNum> right)
        => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TNum operator *(Vector3<TNum> left, Vector3<TNum> right)
        => left.X * right.X + left.Y * right.Y + left.Z * right.Z;

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator *(Vector3<TNum> vec, TNum scalar)
        => new(x: vec.X * scalar, y: vec.Y * scalar, z: vec.Z * scalar);

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator *(TNum scalar, Vector3<TNum> vec)
        => new(vec.X * scalar, vec.Y * scalar, vec.Z * scalar);
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator - (Vector3<TNum> vec)=>new(-vec.X, -vec.Y, -vec.Z);

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator /(Vector3<TNum> vec, TNum divisor)
        => vec * (TNum.One / divisor);


    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator ^(Vector3<TNum> left, Vector3<TNum> right)
        => new(
            x: left.Y * right.Z - left.Z * right.Y,
            y: left.Z * right.X - left.X * right.Z,
            z: left.X * right.Y - left.Y * right.X
        );

    [Pure,SuppressMessage("ReSharper", "CompareOfTNumsByEqualityOperator")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vector3<TNum> left, Vector3<TNum> right)
        => left.X == right.X && left.Y == right.Y && left.Z == right.Z;

    [Pure,SuppressMessage("ReSharper", "CompareOfTNumsByEqualityOperator")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vector3<TNum> left, Vector3<TNum> right)
        => left.X != right.X || left.Y != right.Y || left.Z != right.Z;


    
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<TNum> Add(Vector3<TNum> other)
        => new(X+other.X,Y+other.Y,Z+other.Z);
    


    
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<TNum> Subtract(Vector3<TNum> other)
        => this - other;

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<TNum> Scale(TNum scalar)
        => this * scalar;

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<TNum> Divide(TNum divisor)
        => this / divisor;


    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum Dot(Vector3<TNum> other) => this * other;

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum Distance(Vector3<TNum> other) => (this - other).Length;

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<TNum> Cross(Vector3<TNum> other) => this ^ other;

    
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsParallelTo(Vector3<TNum> other, TNum tolerance) 
        => tolerance>=TNum.Abs(Normalized * other.Normalized);

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsParallelTo(Vector3<TNum> other)
        =>IsParallelTo(other, TNum.Epsilon);

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Vector3<TNum> other)
        => this == other;

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(Vector3<TNum> other)
        => SquaredLength.CompareTo(other.SquaredLength);

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ReadOnlySpan<TNum> AsSpan()
    {
        fixed (TNum* ptr = &X) return new ReadOnlySpan<TNum>(ptr, 3);
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? other)
        => other is Vector3<TNum> vec && vec == this;

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    [Pure]
    public unsafe TNum this[int index]
    {
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (index.InsideInclusiveRange(0, 2))
                fixed (TNum* ptr = &X)
                    return ptr[index];
            throw new IndexOutOfRangeException();
        }
    }

    public TNum this[CoordinateAxis axis]
    =>this[(int)axis];

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out TNum x, out TNum y, out TNum z)
    {
        x = X;
        y = Y;
        z = Z;
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> Lerp(Vector3<TNum> from, Vector3<TNum> to, TNum normalDistance)
        =>(to-from)*normalDistance+from;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> CosineLerp(Vector3<TNum> from, Vector3<TNum> to, TNum normalDistance)
    {
        var two = TNum.CreateTruncating(2);
        normalDistance = normalDistance.Wrap(TNum.Zero, two);
        var sineDistance= TNum.Sin(normalDistance * TNum.Pi / two);
        sineDistance = TNum.Clamp(sineDistance, TNum.Zero, TNum.One);
        return Lerp(from, to, sineDistance);
    }


    [SuppressMessage("ReSharper", "UseStringInterpolation")]
    public override string ToString()
        => string.Format("{{X:{0:F3} Y:{1:F3} Z:{2:F3}}}", X, Y, Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNaN(Vector3<TNum> vec)
    => TNum.IsNaN(vec.X)||TNum.IsNaN(vec.Y)||TNum.IsNaN(vec.Z);
}
