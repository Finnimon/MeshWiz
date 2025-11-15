using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace MeshWiz.Utility.Extensions;

public static class NumExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InsideInclusiveRange<TNum>(this TNum value, TNum min, TNum max)
        where TNum : IBinaryInteger<TNum>, IUnsignedNumber<TNum>
        => value - min <= max - min;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool OutsideInclusiveRange<TNum>(this TNum value, TNum min, TNum max)
        where TNum : IBinaryInteger<TNum>, IUnsignedNumber<TNum>
        => value - min > max - min;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool OutsideInclusiveRange(this sbyte value, sbyte min, sbyte max)
        => ((byte)value).OutsideInclusiveRange((byte)min, (byte)max);

    public static bool InsideInclusiveRange(this sbyte value, sbyte min, sbyte max)
        => ((byte)value).InsideInclusiveRange((byte)min, (byte)max);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool OutsideInclusiveRange(this short value, short min, short max)
        => ((ushort)value).OutsideInclusiveRange((ushort)min, (ushort)max);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InsideInclusiveRange(this short value, short min, short max)
        => ((ushort)value).InsideInclusiveRange((ushort)min, (ushort)max);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InsideInclusiveRange(this int value, int min, int max)
        => ((uint)value).InsideInclusiveRange((uint)min, (uint)max);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool OutsideInclusiveRange(this int value, int min, int max)
        => ((uint)value).OutsideInclusiveRange((uint)min, (uint)max);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InsideInclusiveRange(this long value, long min, long max)
        => ((ulong)value).InsideInclusiveRange((ulong)min, (ulong)max);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool OutsideInclusiveRange(this long value, long min, long max)
        => ((ulong)value).OutsideInclusiveRange((ulong)min, (ulong)max);

    [Pure]
    public static TNum Wrap<TNum>(this TNum value, TNum min, TNum max)
        where TNum : INumber<TNum>
    {
        var range = max - min;
        if (range == TNum.Zero)
            return min; // or throw

        return (((value - min) % range + range) % range) + min;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TNum WrapSaturating<TNum>(this TNum value)
        where TNum : INumber<TNum>
        => value.Wrap(TNum.Zero, TNum.One);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsApprox<TNum>(this TNum num, TNum other, TNum epsilon)
        where TNum : IFloatingPoint<TNum>
        => TNum.Abs(num - other) < epsilon;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsApprox<TNum>(this TNum num, TNum other)
        where TNum : IFloatingPointIeee754<TNum>
        => TNum.Abs(num - other) < Numbers<TNum>.ZeroEpsilon;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsApproxGreaterOrEqual<TNum>(this TNum num, TNum other)
        where TNum : INumberBase<TNum>, IComparisonOperators<TNum, TNum, bool>
    {
        var diff = num - other;
        return diff >= -Numbers<TNum>.ZeroEpsilon;
    }


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsApproxZero<TNum>(this TNum num)
        where TNum : INumber<TNum>
        => TNum.Abs(num) < Numbers<TNum>.ZeroEpsilon;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EpsilonTruncatingSign<TNum>(this TNum num, TNum epsilon)
        where TNum : IFloatingPoint<TNum>
    {
#if DEBUG
        if (epsilon < TNum.Zero) throw new ArgumentOutOfRangeException();
#endif
        if (num > epsilon) return 1;
        if (num < -epsilon) return -1;
        return 0;
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EpsilonTruncatingSign<TNum>(this TNum num)
        where TNum : IFloatingPointIeee754<TNum>
        => num.EpsilonTruncatingSign(Numbers<TNum>.ZeroEpsilon);

    public static bool InsideRange<TNum>(this TNum value, TNum min, TNum max)
        where TNum : IFloatingPointIeee754<TNum> =>
        value >= min - TNum.Epsilon && value <= max + TNum.Epsilon;

    public static TNum NextPow2<TNum>(this TNum num)
        where TNum : IBinaryInteger<TNum>
    {
        var halfBitSize = Unsafe.SizeOf<TNum>() * 4;
        --num;
        for (var shift = 1; shift <= halfBitSize; shift *= 2)
            num |= num >> shift;
        return ++num;
    }
}