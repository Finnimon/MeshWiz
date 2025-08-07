using System.Diagnostics.Contracts;
using System.Numerics;
using MeshWiz.Contracts;

namespace MeshWiz.Math;

public interface IVector<TSelf, TNum>
    : IReadOnlyList<TNum>,
        IEquatable<TSelf>,
        IComparable<TSelf>,
        IUnmanagedDataVector<TNum>,
        IEqualityOperators<TSelf,TSelf,bool>,
        IAdditionOperators<TSelf,TSelf,TSelf>,
        ISubtractionOperators<TSelf,TSelf,TSelf>,
        IFormattable,
        IUnaryNegationOperators<TSelf,TSelf>,
        IMultiplyOperators<TSelf, TNum, TSelf>,
        IDivisionOperators<TSelf,TNum,TSelf>
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
    [Pure] TNum SquaredLength { get; }
    [Pure] TSelf Normalized { get; }

    [Pure]
    TSelf Add(TSelf other);

    [Pure]
    TSelf Subtract(TSelf other);

    [Pure]
    TSelf Scale(TNum scalar);

    [Pure]
    TSelf Divide(TNum divisor);

    [Pure]
    TNum Dot(TSelf other);

    [Pure]
    TNum DistanceTo(TSelf other);
    TNum SquaredDistanceTo(TSelf other);

    static abstract TSelf Lerp(TSelf from, TSelf to, TNum normalDistance);

    bool IsParallelTo(TSelf other);
    bool IsParallelTo(TSelf other, TNum tolerance);

    bool IsApprox(TSelf other, TNum squareTolerance)
        => this.SquaredDistanceTo(other) < squareTolerance;

    bool IsApprox(TSelf other);
    
    public static abstract TNum operator *(TSelf left,TSelf right);
}