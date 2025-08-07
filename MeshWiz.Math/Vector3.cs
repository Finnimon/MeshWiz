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

    private Vector3(TNum value) : this(value, value, value) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> FromXYZ(TNum x, TNum y, TNum z)
        => new(x, y, z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> FromComponents<TList>(TList components)
    where TList : IReadOnlyList<TNum>
        =>new(components[0], components[1],components[2]);
    
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> FromComponents<TList,TOtherNum>(TList components)
        where TList : IReadOnlyList<TOtherNum>
        where TOtherNum : INumber<TOtherNum>
        =>new(TNum.CreateTruncating(components[0]), 
            TNum.CreateTruncating(components[1]),
            TNum.CreateTruncating(components[2]));


    public Vector3<TNum> Inverse => Invert(this);


    public Vector3<TNum> ZYX=>new(Z,Y,Y);
    public Vector3<TNum> YZX=>new(Y,Z,Y);
    public Vector3<TNum> YXZ=>new(Y,Y,Z);
    public Vector3<TNum> XZY=>new(Y,Z,Y);
    public Vector3<TNum> ZXY=>new(Z,Y,Y);
    public Vector3<TNum> XXX => new(X);
    public Vector3<TNum> YYY => new(Y);
    public Vector3<TNum> ZZZ => new(Z);

    public static Vector3<TNum> Zero => new(TNum.Zero, TNum.Zero, TNum.Zero);
    public static Vector3<TNum> One => new(TNum.One, TNum.One, TNum.One);
    public static Vector3<TNum> NaN => new(TNum.NaN, TNum.NaN, TNum.NaN);
    public static Vector3<TNum> UnitX=>new(TNum.One,TNum.Zero,TNum.Zero);
    public static Vector3<TNum> UnitY=>new(TNum.Zero,TNum.One,TNum.Zero);
    public static Vector3<TNum> UnitZ=>new(TNum.Zero,TNum.Zero,TNum.One);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<TOther> To<TOther>() where TOther : unmanaged, IFloatingPointIeee754<TOther>
        => new(TOther.CreateTruncating(X), TOther.CreateTruncating(Y),TOther.CreateTruncating(Z));


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator +(Vector3<TNum> left, Vector3<TNum> right)
        => new (left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator -(Vector3<TNum> left, Vector3<TNum> right)
        => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TNum operator *(Vector3<TNum> left, Vector3<TNum> right)
        => left.X * right.X + left.Y * right.Y + left.Z * right.Z;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator *(Vector3<TNum> vec, TNum scalar)
        => new(x: vec.X * scalar, y: vec.Y * scalar, z: vec.Z * scalar);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator *(TNum scalar, Vector3<TNum> vec)
        => new(vec.X * scalar, vec.Y * scalar, vec.Z * scalar);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator - (Vector3<TNum> vec)=>new(-vec.X, -vec.Y, -vec.Z);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator /(Vector3<TNum> vec, TNum divisor)
        => vec * (TNum.One / divisor);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator /(TNum num,Vector3<TNum> vec)
        => new(num/vec.X,num/vec.Y,num/vec.Z);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> operator ^(Vector3<TNum> left, Vector3<TNum> right)
        => new(
            x: left.Y * right.Z - left.Z * right.Y,
            y: left.Z * right.X - left.X * right.Z,
            z: left.X * right.Y - left.Y * right.X
        );

    [Pure, SuppressMessage("ReSharper", "CompareOfTNumsByEqualityOperator"),
     MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vector3<TNum> left, Vector3<TNum> right)
        => left.X == right.X && left.Y == right.Y && left.Z == right.Z;

    [Pure, SuppressMessage("ReSharper", "CompareOfTNumsByEqualityOperator"),
     MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vector3<TNum> left, Vector3<TNum> right)
        => left.X != right.X || left.Y != right.Y || left.Z != right.Z;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<TNum> Add(Vector3<TNum> other)
        => new(X+other.X,Y+other.Y,Z+other.Z);


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<TNum> Subtract(Vector3<TNum> other)
        => this - other;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<TNum> Scale(TNum scalar)
        => this * scalar;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<TNum> Divide(TNum divisor)
        => this / divisor;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum Dot(Vector3<TNum> other) => this * other;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum DistanceTo(Vector3<TNum> other) => (this - other).Length;
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum SquaredDistanceTo(Vector3<TNum> other) => (this-other).SquaredLength;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3<TNum> Cross(Vector3<TNum> other) => this ^ other;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsParallelTo(Vector3<TNum> other, TNum tolerance) 
        => tolerance>= TNum.Abs(TNum.Abs(Normalized * other.Normalized) - TNum.One);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsParallelTo(Vector3<TNum> other)
        =>IsParallelTo(other, TNum.Epsilon);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Vector3<TNum> other)
        => this == other;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(Vector3<TNum> other)
        => SquaredLength.CompareTo(other.SquaredLength);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ReadOnlySpan<TNum> AsSpan()
    {
        fixed (TNum* ptr = &X) return new ReadOnlySpan<TNum>(ptr, 3);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? other)
        => other is Vector3<TNum> vec && vec == this;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    [Pure]
    public unsafe TNum this[int index]
    {
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out TNum x, out TNum y, out TNum z)
    {
        x = X;
        y = Y;
        z = Z;
    }

    
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> Lerp(Vector3<TNum> from, Vector3<TNum> to, TNum normalDistance)
        =>(to-from)*normalDistance+from;
    
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> CosineLerp(Vector3<TNum> from, Vector3<TNum> to, TNum normalDistance)
    {
        var two = TNum.CreateTruncating(2);
        normalDistance = normalDistance.Wrap(TNum.Zero, two);
        var sineDistance= TNum.Sin(normalDistance * TNum.Pi / two);
        sineDistance = TNum.Clamp(sineDistance, TNum.Zero, TNum.One);
        return Lerp(from, to, sineDistance);
    }
    
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> Invert(Vector3<TNum> vec)
        => TNum.One / vec;

    public static Vector3<TNum> Pow(Vector3<TNum> v,TNum power)
    =>new(TNum.Pow(v.X,power),
        TNum.Pow(v.Y,power),
        TNum.Pow(v.Z,power));
    
    public static Vector3<TNum> Squared(Vector3<TNum> v)
    =>new(v.X*v.X,v.Y*v.Y,v.Z*v.Z);
    
    public static Vector3<TNum> ElementWiseMul(Vector3<TNum> l, Vector3<TNum>r)
    =>new(l.X*r.X,l.Y*r.Y,l.Z*r.Z);
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> Min(Vector3<TNum> left, Vector3<TNum> right)
        =>new(TNum.Min(left.X,right.X),TNum.Min(left.Y,right.Y),TNum.Min(left.Z,right.Z));
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3<TNum> Max(Vector3<TNum> left, Vector3<TNum> right)
        =>new(TNum.Max(left.X,right.X),TNum.Max(left.Y,right.Y),TNum.Max(left.Z,right.Z));

    [SuppressMessage("ReSharper", "UseStringInterpolation")]
    public override string ToString()
        => $"{nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Z)}: {Z}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNaN(Vector3<TNum> vec)
    => TNum.IsNaN(vec.X)||TNum.IsNaN(vec.Y)||TNum.IsNaN(vec.Z);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ray3<TNum> RayThrough(Vector3<TNum> through) => new Ray3<TNum>(this, through - this);
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Line<Vector3<TNum>,TNum> LineTo(Vector3<TNum> end) => new (this, end);
    
    
    
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsApprox(Vector3<TNum> other, TNum squareTolerance) => SquaredDistanceTo(other) < squareTolerance;
    
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsApprox(Vector3<TNum> other)=>SquaredDistanceTo(other)<=TNum.Epsilon;

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        FormattableString formattable = $"{nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Z)}: {Z}";
        return formattable.ToString(formatProvider);
    }
}
