using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Angle<TNum>
where TNum: unmanaged, IFloatingPointIeee754<TNum>
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

    /// <summary>
    /// Checks if this angle is in <paramref name="boundary"/> considering the equivalence of a+Pi*2==a
    /// </summary>
    /// <param name="boundary">boundary to check</param>
    /// <returns>whether this is contained in the angular <paramref name="boundary"/></returns>
    [Pure]
    public bool IsIn(AABB<TNum> boundary)
    {
        if (boundary.IsNegativeSpace) return false;
        var twoPi = Numbers<TNum>.TwoPi;
        if (boundary.Size.IsApproxGreaterOrEqual(twoPi)) return true;
        var directCheck= boundary.Contains(this);
        if (directCheck) return true;
        TNum angle = this;
        var diff = boundary.Min - angle;
        var totalRotationsDiff = diff % twoPi;
        totalRotationsDiff += diff >= TNum.Zero ? TNum.One : TNum.NegativeOne;
        angle += totalRotationsDiff * twoPi;
        return boundary.Contains(angle);
    }
}