using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.Utility;

public static class Enums
{
    public static bool HasAnyFlags<T>(this T target, T flags)
        where T : unmanaged,Enum => !UnsafeAnd(target,flags).Equals(default(T));

    
    public static T UnsafeAnd<T>(T a, T b)
        where T : unmanaged, Enum
        => Unsafe.SizeOf<T>() switch
        {
            1 => UnsafeAnd<T, byte>(a, b),
            2 => UnsafeAnd<T, ushort>(a, b),
            4 => UnsafeAnd<T, uint>(a, b),
            8 => UnsafeAnd<T, ulong>(a, b),
            16 => UnsafeAnd<T, Int128>(a, b),
            _ => ThrowHelper.ThrowInvalidOperationException<T>()
        };

    private static TSource UnsafeAnd<TSource, TOperator>(TSource a, TSource b)
        where TSource : unmanaged
        where TOperator : IBitwiseOperators<TOperator, TOperator, TOperator>
        => Unsafe.BitCast<TOperator, TSource>(Unsafe.BitCast<TSource, TOperator>(a) &
                                              Unsafe.BitCast<TSource, TOperator>(b));

    /// <summary>
    /// When types are known at compile time prefer direct comparisons
    /// </summary>
    [Pure]
    public static unsafe bool AreEqual<T>(T a, T b)
        where T : unmanaged, Enum =>
        Unsafe.SizeOf<T>() switch
        {
            0 => true,
            1 => UnsafeEqual<T, byte>(ref a, ref b),
            2 => UnsafeEqual<T, ushort>(ref a, ref b),
            4 => UnsafeEqual<T, uint>(ref a, ref b),
            8 => UnsafeEqual<T, ulong>(ref a, ref b),
            16 => UnsafeEqual<T, Int128>(ref a, ref b),//futureproofing
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            _ => a.Equals(b)
        };

    [Pure]
    private static unsafe bool UnsafeEqual<TSource, TTarget>(ref TSource a, ref TSource b)
        where TTarget : unmanaged, IEqualityOperators<TTarget, TTarget, bool> where TSource : unmanaged =>
        Unsafe.As<TSource, TTarget>(ref a) == Unsafe.As<TSource, TTarget>(ref b);

    internal static bool IsSuccess<T>(T value) where T : unmanaged, Enum
        => AreEqual(value, EnumResultHelper<T>.SuccessConstant);
}