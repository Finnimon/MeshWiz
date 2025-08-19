using System.Diagnostics.Contracts;
using System.Numerics;
using MeshWiz.Contracts;

namespace MeshWiz.Math;

public interface IVector<TSelf, TNum>
    : IReadOnlyList<TNum>,
        IUnmanagedDataVector<TNum>,
        INumber<TSelf> 
    where TNum : unmanaged, INumber<TNum>
    where TSelf : IVector<TSelf, TNum>
{
    [Pure]
    static abstract TSelf FromComponents<TList>(TList components) where TList : IReadOnlyList<TNum>;

    [Pure]
    static abstract TSelf FromComponents<TList, TOtherNum>(TList components)
        where TList : IReadOnlyList<TOtherNum>
        where TOtherNum : INumber<TOtherNum>;

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

    [Pure]
    static abstract TSelf Lerp(TSelf from, TSelf to, TNum t);

    [Pure]
    static abstract TSelf ExactLerp(TSelf from, TSelf toward, TNum exactDistance);

    [Pure]
    bool IsParallelTo(TSelf other);
    bool IsParallelTo(TSelf other, TNum tolerance);

    [Pure]
    bool IsApprox(TSelf other, TNum squareTolerance)
        => this.SquaredDistanceTo(other) < squareTolerance;

    [Pure]
    bool IsApprox(TSelf other);

    [Pure]
    public static abstract TSelf operator *(TSelf l, TNum r);
    [Pure]
    public static abstract TSelf operator *(TNum l, TSelf r);
    [Pure]
    public static abstract TSelf operator /(TSelf l, TNum r);
    [Pure]
    public static abstract TSelf operator /(TNum l, TSelf r);

    

    [Pure]
    public TNum Sum { get; }
}