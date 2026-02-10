using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MeshWiz.Buffers;

internal static class Utilities
{
    [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
    internal static int GetWordCount<T>(int length)
    {
        var size = Unsafe.SizeOf<T>() * (long)length;
        var align = nint.Size;
        size = (size + align - 1) / align * align;
        return (int)(size / align);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
    internal static int GetWordCount<T>(long length)
    {
        var size = Unsafe.SizeOf<T>() * length;
        var align = nint.Size;
        size = (size + align - 1) / align * align;
        return (int)(size / align);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
    internal static int GetElemCount<T>(long wordCount) => (int)((wordCount * nint.Size) / Unsafe.SizeOf<T>());


    public enum MemoryPressure
    {
        Low,
        High,
        Medium
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<TTo> UnsafeCast<TTo>(nuint[] span)
    {
        uint num1 = (uint)nuint.Size;
        uint num2 = (uint)Unsafe.SizeOf<TTo>();
        uint length1 = (uint)span.Length;
        int length2 = (int)num1 != (int)num2
            ? (num1 != 1U ? checked((int)unchecked((ulong)length1 * (ulong)num1 / (ulong)num2)) : (int)(length1 / num2))
            : (int)length1;
        return MemoryMarshal.CreateSpan(ref Unsafe.As<nuint, TTo>(ref MemoryMarshal.GetArrayDataReference(span)),
            length2);
    }
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // internal static Span<TTo> UnsafeCast<TTo>(Span<nuint> span)
    // {
    //     uint num1 = (uint)nint.Size;
    //     uint num2 = (uint)Unsafe.SizeOf<TTo>();
    //     uint length1 = (uint)span.Length;
    //     int length2 = (int)num1 != (int)num2
    //         ? (num1 != 1U ? checked((int)unchecked((ulong)length1 * (ulong)num1 / (ulong)num2)) : (int)(length1 / num2))
    //         : (int)length1;
    //     return MemoryMarshal.CreateSpan(ref Unsafe.As<nuint, TTo>(ref MemoryMarshal.GetReference(span)),
    //         length2);
    // }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<T> Resize<T>(Span<T> span, int newSize) => MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(span), newSize);

    public static MemoryPressure GetMemoryPressure()
    {
        var gcMemoryInfo = GC.GetGCMemoryInfo();
        if (gcMemoryInfo.MemoryLoadBytes >= gcMemoryInfo.HighMemoryLoadThresholdBytes * 0.9)
            return MemoryPressure.High;
        return gcMemoryInfo.MemoryLoadBytes >= gcMemoryInfo.HighMemoryLoadThresholdBytes * 0.7
            ? MemoryPressure.Medium
            : MemoryPressure.Low;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NextPow2(this int num)
    {
        var halfBitSize = sizeof(int) * 4;
        --num;
        for (var shift = 1; shift <= halfBitSize; shift *= 2)
            num |= num >> shift;
        return ++num;
    }
}