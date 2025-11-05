using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace MeshWiz.Utility;

public static class Enums
{
    /// <summary>
    /// When types are known at compile time prefer direct comparisons
    /// </summary>
    [Pure]
    public static unsafe bool AreEqual<T>(T a, T b)
        where T : unmanaged, Enum =>
        sizeof(T) switch
        {
            0 => true,
            1 => UnsafeEqual<T, byte>(ref a, ref b),
            2 => UnsafeEqual<T, ushort>(ref a, ref b),
            4 => UnsafeEqual<T, uint>(ref a, ref b),
            8 => UnsafeEqual<T, ulong>(ref a, ref b),
            16 => UnsafeEqual<T, Int128>(ref a, ref b),
            // ReSharper disable once HeapView.PossibleBoxingAllocation
            _ => a.Equals(b)
        };

    [Pure]
    private static unsafe bool UnsafeEqual<TSource, TTarget>(ref TSource a, ref TSource b)
        where TTarget : unmanaged, IEqualityOperators<TTarget, TTarget, bool> where TSource : unmanaged =>
        Unsafe.As<TSource, TTarget>(ref a) == Unsafe.As<TSource, TTarget>(ref b);

    internal static bool IsSuccess<T>(T value) where T : unmanaged, Enum
        => AreEqual(value, ResultHelper<T>.SuccessConstant);
}