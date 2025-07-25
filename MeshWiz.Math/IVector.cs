using System.Diagnostics.Contracts;
using System.Numerics;
using MeshWiz.Contracts;

namespace MeshWiz.Math;

public interface IVector<TSelf, TNum>
    : IReadOnlyList<TNum>,
        IEquatable<TSelf>,
        IComparable<TSelf>,
        IUnmanagedDataVector<TNum>
    where TNum : unmanaged, INumber<TNum>
    where TSelf : IVector<TSelf, TNum>
{
    static abstract TSelf FromComponents<TList>(TList components) where TList : IReadOnlyList<TNum>;

    static abstract TSelf FromComponents<TList, TOtherNum>(TList components)
        where TList : IReadOnlyList<TOtherNum>
        where TOtherNum : INumber<TOtherNum>;

    static abstract TSelf Zero { get; }
    static abstract TSelf One { get; }
    [Pure] static abstract uint Dimensions { get; }
    [Pure] int IReadOnlyCollection<TNum>.Count => (int)TSelf.Dimensions;
    [Pure] TNum Length { get; }
    [Pure] TNum SquaredLength => Dot((TSelf)this);
    [Pure] TSelf Normalized => this.Divide(Length);

    [Pure]
    TSelf Add(TSelf other);

    [Pure]
    TSelf Subtract(TSelf other) => this.Add(other.Scale(-TNum.One));

    [Pure]
    TSelf Scale(TNum scalar);

    [Pure]
    TSelf Divide(TNum divisor) => Scale(TNum.One / divisor);

    [Pure]
    TNum Dot(TSelf other);

    [Pure]
    TNum DistanceTo(TSelf other) => Subtract(other).Length;
    TNum SquaredDistanceTo(TSelf other) => Subtract(other).SquaredLength;
    static virtual TSelf operator +(TSelf left, TSelf right) => left.Add(right);
    static virtual TSelf operator -(TSelf left, TSelf right) => left.Subtract(right);
    static virtual TNum operator *(TSelf left, TSelf right) => left.Dot(right);
    static virtual TSelf operator *(TNum scalar, TSelf vector) => vector.Scale(scalar);
    static virtual TSelf operator *(TSelf vector, TNum scalar) => vector.Scale(scalar);
    static virtual TSelf operator /(TSelf vector, TNum divisor) => vector.Divide(divisor);
    static virtual bool operator ==(TSelf vector, TSelf divisor) => vector.Equals(divisor);
    static virtual bool operator !=(TSelf vector, TSelf divisor) => !vector.Equals(divisor);
    static virtual TSelf operator -(TSelf vector) => vector.Scale(-TNum.One);

    static virtual TSelf Lerp(TSelf from, TSelf to, TNum normalDistance)
        => (to - from) * normalDistance + from;


    bool IsParallelTo(TSelf other);
    bool IsParallelTo(TSelf other, TNum tolerance);

    bool IsApprox(TSelf other, TNum squareTolerance)
        => this.SquaredDistanceTo(other) < squareTolerance;

    bool IsApprox(TSelf other);
}