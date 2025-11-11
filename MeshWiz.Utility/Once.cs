using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MeshWiz.Utility;

[StructLayout(LayoutKind.Sequential,Size=1)]
public struct Once(bool b)
{
    private byte _value = (byte)(b ? 0b01 : 0b00);
    private const byte ConsumedFlag = 0b10;
    private const byte ValueFlag = 0b01;
    public static Once True { get; } = new Once(true);
    public static Once False { get; } = new Once(false);



    public bool CurrentValue => GetValue(_value);
    [MethodImpl(MethodImplOptions.AggressiveInlining),Pure]
    private static bool IsUnconsumed(byte value) => (value & ConsumedFlag)!=ConsumedFlag;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(in Once once)
    {
        var value = GetValue(once._value);
        if (IsUnconsumed(once._value)) 
            ConsumeRare(ref Unsafe.AsRef(in once));
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining),Pure]
    public static bool GetValue(byte value) 
        => (value & ValueFlag) == ValueFlag;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ConsumeRare(ref Once ptr)
    {
        const int mask = 0b11;
        ptr._value = (byte)(mask& ~ptr._value);
    }
}