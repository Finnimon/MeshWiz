using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace MeshWiz.Math;

public static class Vec<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TVec Mul<TVec>(TVec a, TVec b)
        where TVec : unmanaged, IVecBase<TVec, TNum>
    {
        Unsafe.SkipInit<TVec>(out var res);
        for (var i = 0; i < TVec.Dimensions; i++)
            SetElement(in res, i, GetElement(in a, i) * GetElement(in b, i));
        return res;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
    internal static TNum GetElement<TVec>(in TVec a, int index)
        where TVec : unmanaged, IVecBase<TVec, TNum>
        => Unsafe.Add(ref Unsafe.As<TVec, TNum>(ref Unsafe.AsRef(in a)), index);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetElement<TVec>(in TVec a, int index, TNum value)
        where TVec : unmanaged, IVecBase<TVec, TNum>
        => Unsafe.Add(ref Unsafe.As<TVec, TNum>(ref Unsafe.AsRef(in a)), index) = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
    internal static ref TNum Pin<TVec>(in TVec v)
        => ref Unsafe.As<TVec, TNum>(ref Unsafe.AsRef(in v));
}

public static class Mat<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
    internal static TNum GetElement<TMat, TRow, TCol>(in TMat a, int row, int col)
        where TMat : unmanaged, IMat<TMat, TRow, TCol, TNum>
        where TRow : unmanaged, IVec<TRow, TNum>
        where TCol : unmanaged, IVec<TCol, TNum>
        => Unsafe.Add(ref Unsafe.As<TMat, TNum>(ref Unsafe.AsRef(in a)), row * TRow.Dimensions + col);

    [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
    internal static TNum GetElement<TMat, TVec>(in TMat a, int row, int col)
        where TMat : unmanaged, IMat<TMat, TVec, TVec, TNum>
        where TVec : unmanaged, IVec<TVec, TNum>
        => Unsafe.Add(ref Unsafe.As<TMat, TNum>(ref Unsafe.AsRef(in a)), row * TVec.Dimensions + col);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetElement<TMat, TRow, TCol>(in TMat a, int row, int col, TNum v)
        where TMat : unmanaged, IMat<TMat, TRow, TCol, TNum>
        where TRow : unmanaged, IVec<TRow, TNum>
        where TCol : unmanaged, IVec<TCol, TNum>
        => Unsafe.Add(ref Unsafe.As<TMat, TNum>(ref Unsafe.AsRef(in a)), row * TRow.Dimensions + col) = v;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetElement<TMat, TVec>(in TMat a, int row, int col, TNum v)
        where TMat : unmanaged, IMat<TMat, TVec, TVec, TNum>
        where TVec : unmanaged, IVec<TVec, TNum>
        => Unsafe.Add(ref Unsafe.As<TMat, TNum>(ref Unsafe.AsRef(in a)), row * TVec.Dimensions + col) = v;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetRow<TMat, TRow, TCol>(in TMat a, int row, TRow v)
        where TMat : unmanaged, IMat<TMat, TRow, TCol, TNum>
        where TRow : unmanaged, IVec<TRow, TNum>
        where TCol : unmanaged, IVec<TCol, TNum>
        => Unsafe.Add(ref Unsafe.As<TMat, TRow>(ref Unsafe.AsRef(in a)), row) = v;

    [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
    internal static TRow GetRow<TMat, TRow, TCol>(in TMat a, int row)
        where TMat : unmanaged, IMat<TMat, TRow, TCol, TNum>
        where TRow : unmanaged, IVec<TRow, TNum>
        where TCol : unmanaged, IVec<TCol, TNum>
        => Unsafe.Add(ref Unsafe.As<TMat, TRow>(ref Unsafe.AsRef(in a)), row);

    [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
    internal static TCol GetCol<TMat, TRow, TCol>(in TMat a, int col)
        where TMat : unmanaged, IMat<TMat, TRow, TCol, TNum>
        where TRow : unmanaged, IVec<TRow, TNum>
        where TCol : unmanaged, IVec<TCol, TNum>
    {
        Unsafe.SkipInit<TCol>(out var v);
        ref var pin = ref Unsafe.As<TMat, TNum>(ref Unsafe.AsRef(in a));
        var rowDim = TRow.Dimensions;
        var colDim = TCol.Dimensions;
        for (var row = 0; row < colDim; row++) 
            Vec<TNum>.SetElement(in v, row, Unsafe.Add(ref pin, row * rowDim + col));
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
    internal static TVec GetCol<TMat, TVec>(in TMat a, int col)
        where TMat : unmanaged, IMat<TMat, TVec, TVec, TNum>
        where TVec : unmanaged, IVec<TVec, TNum>
        => GetCol<TMat, TVec, TVec>(in a, col);
    [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
    internal static TVec GetRow<TMat, TVec>(in TMat a, int row)
        where TMat : unmanaged, IMat<TMat, TVec, TVec, TNum>
        where TVec : unmanaged, IVec<TVec, TNum>
        => GetRow<TMat, TVec, TVec>(in a, row);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetCol<TMat, TRow, TCol>(in TMat a, int col, TCol v)
        where TMat : unmanaged, IMat<TMat, TRow, TCol, TNum>
        where TRow : unmanaged, IVec<TRow, TNum>
        where TCol : unmanaged, IVec<TCol, TNum>
    {
        ref var pin = ref Unsafe.As<TMat, TNum>(ref Unsafe.AsRef(in a));
        var rowDim = TRow.Dimensions;
        var colDim = TCol.Dimensions;
        for (var row = 0; row < colDim; row++)
            Unsafe.Add(ref pin, row * rowDim + col) = Vec<TNum>.GetElement(in v, row);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetCol<TMat, TVec>(in TMat a, int col,TVec v)
        where TMat : unmanaged, IMat<TMat, TVec, TVec, TNum>
        where TVec : unmanaged, IVec<TVec, TNum>
        => SetCol<TMat, TVec, TVec>(in a, col,v);
}