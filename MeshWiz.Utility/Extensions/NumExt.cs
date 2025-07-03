using System.Numerics;
using System.Runtime.CompilerServices;

namespace MeshWiz.Utility.Extensions;

public static class NumExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InsideInclusiveRange(this int value, int min, int max)
    => (uint)(value - min) <= (uint)(max - min);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool OutsideInclusiveRange(this int value, int min, int max)
    => (uint)(value - min) > (uint)(max - min);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InsideInclusiveRange(this long value, long min, long max)
    => (ulong)(value - min) <= (ulong)(max - min);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool OutsideInclusiveRange(this long value, long min, long max)
    => (ulong)(value - min) > (ulong)(max - min);
    
    public static TNum Wrap<TNum>(this TNum value, TNum min, TNum max)
    where TNum:unmanaged,INumber<TNum>
    {
        var range = max - min;
        if (range == TNum.Zero)
            return min; // or throw

        return (((value - min) % range + range) % range) + min;
    }
}
