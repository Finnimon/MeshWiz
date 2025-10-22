using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MeshWiz.Utility;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Angle<TNum>
where TNum:INumberBase<TNum>
{
    public readonly TNum Radians;
    public TNum Degrees => Radians * Numbers<TNum>.RadiansToDegree;
    private Angle(TNum rad)=>Radians = rad;
    public static implicit operator TNum(Angle<TNum> angle) => angle.Radians;
    public static implicit operator Angle<TNum>(TNum radians)=>Unsafe.As<TNum, Angle<TNum>>(ref radians);
    public static Angle<TNum> FromRadians(TNum radians) => new(radians);
    public static Angle<TNum> FromDegrees(TNum degrees) => new(Numbers<TNum>.DegreeToRadians * degrees);

    /// <inheritdoc />
    public override string ToString()
    {
        const string formattable = "{0}°";
        return string.Format(formattable, Degrees);
    }
}