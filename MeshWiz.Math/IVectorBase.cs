using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using SysNum = System.Numerics;
using MeshWiz.Contracts;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public interface IVectorBase<TSelf, TNum>
    : IUnmanagedDataVector<TNum>,
        IReadOnlyList<TNum>,
        SysNum.IAdditionOperators<TSelf, TSelf, TSelf>,
        SysNum.ISubtractionOperators<TSelf, TSelf, TSelf>,
        SysNum.IMultiplicativeIdentity<TSelf, TSelf>,
        SysNum.IMultiplyOperators<TSelf, TSelf, TSelf>,
        SysNum.IUnaryNegationOperators<TSelf, TSelf>,
        SysNum.IUnaryPlusOperators<TSelf, TSelf>,
        SysNum.IDecrementOperators<TSelf>,
        SysNum.IDivisionOperators<TSelf, TSelf, TSelf>,
        IEquatable<TSelf>,
        SysNum.IIncrementOperators<TSelf>,
        IComparable,
        IComparable<TSelf>,
        SysNum.IComparisonOperators<TSelf, TSelf, bool>,
        IPosition<TSelf,TSelf, TNum>,
        ILerp<TSelf,TNum> ,
        IByteSize
    where TSelf : unmanaged, IVectorBase<TSelf, TNum>
    where TNum : unmanaged, SysNum.IFloatingPointIeee754<TNum>
{
    /// <inheritdoc />
    int IReadOnlyCollection<TNum>.Count => TSelf.Dimensions;

    static virtual int Dimensions => Unsafe.SizeOf<TSelf>() / Unsafe.SizeOf<TNum>();
    TNum Length { get; }
    TNum SquaredLength => TSelf.Dot((TSelf)this, (TSelf)this);

    [Pure] public TNum Sum => this.Sum();


    [Pure]
    static virtual TNum Dot(TSelf a, TSelf b)
    {
        var result = TNum.Zero;
        for (var i = 0; i < TSelf.Dimensions; i++)
            result += a[i] * b[i];
        return result;
    }

    [Pure]
    TNum Dot(TSelf other) => TSelf.Dot(other, (TSelf)this);

    [Pure]
    static virtual bool ArePerpendicular(TSelf a, TSelf b)
        => TSelf.Dot(a, b).IsApproxZero();

    [Pure]
    static virtual Angle<TNum> AngleBetween(TSelf a, TSelf b)
    {
        a = TSelf.Normalize(a);
        b = TSelf.Normalize(b);
        var dot = TSelf.Dot(a, b);
        return TNum.Acos(dot);
    }

    [Pure]
    TNum AngleTo(TSelf other) => TSelf.AngleBetween((TSelf)this, other);

    [Pure]
    static virtual TSelf Normalize(TSelf v) => v / v.Length;

    [Pure]
    TSelf Normalized() => TSelf.Normalize((TSelf)this);

    [Pure]
    static abstract TSelf operator *(TSelf l, TNum r);

    [Pure]
    static abstract TSelf operator *(TNum l, TSelf r);

    [Pure]
    static abstract TSelf operator /(TSelf l, TNum r);

    [Pure]
    static abstract TSelf operator /(TNum l, TSelf r);

    [Pure]
    bool IsParallelTo(TSelf other)
    {
        var v1 = this.Normalized();
        var v2 = other.Normalized();
        return TNum.Abs(TSelf.Dot(v1, v2)).IsApprox(TNum.One);
    }

    [Pure]
    bool IsApprox(TSelf other, TNum squareTolerance)
        => SquaredDistanceTo(other) < squareTolerance;

    [Pure]
    bool IsApprox(TSelf other) => IsApprox(other, Numbers<TNum>.ZeroEpsilon);


    [Pure]
    static abstract TSelf FromComponents<TList>(TList components) where TList : IReadOnlyList<TNum>;

    [Pure]
    static virtual TSelf FromComponents<TList, TOtherNum>(TList components)
        where TList : IReadOnlyList<TOtherNum>
        where TOtherNum : SysNum.INumberBase<TOtherNum>
        => TSelf.FromComponents(components.Select(TNum.CreateTruncating).ToArray());

    [Pure]
    static virtual TSelf FromComponentsConstrained<TList, TOtherNum>(TList components)
        where TList : IReadOnlyList<TOtherNum>
        where TOtherNum : SysNum.INumberBase<TOtherNum>
        => TSelf.FromComponentsConstrained(components.Select(TNum.CreateTruncating).ToArray());

    [Pure]
    static abstract TSelf FromComponentsConstrained<TList>(TList components) where TList : IReadOnlyList<TNum>;

    [Pure]
    static abstract TSelf FromValue(TNum value);

    [Pure]
    static virtual TSelf FromValue<TOtherNum>(TOtherNum other)
        where TOtherNum : SysNum.INumberBase<TOtherNum>
        => TSelf.FromValue(TNum.CreateTruncating(other));

    /// <inheritdoc />
    TSelf IPosition<TSelf, TSelf, TNum>.Position => (TSelf)this;
}

public interface IDistance<TSelf, TNum>
    where TSelf : IDistance<TSelf, TNum>
{
    [Pure]
    TNum DistanceTo(TSelf other);

    [Pure]
    TNum SquaredDistanceTo(TSelf other);

    [Pure]
    static abstract TNum Distance(TSelf a, TSelf b);
    
    [Pure]
    static abstract TNum SquaredDistance(TSelf a, TSelf b);
}

public interface IPosition<TSelf, out TPosition, TNum> : IDistance<TSelf, TNum>
    where TSelf : IPosition<TSelf, TPosition, TNum>
    where TPosition : IPosition<TPosition, TPosition, TNum>
{
    TPosition Position { get; }
}

public interface ILerp<TSelf, TNum> : IDistance<TSelf, TNum>
    where TSelf : ILerp<TSelf, TNum>
{
    [Pure]
    static abstract TSelf Lerp(TSelf a, TSelf b, TNum t);

    [Pure]
    static abstract TSelf ExactLerp(TSelf a, TSelf b, TNum exactDistance);
}