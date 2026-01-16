using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct BvhHitInfo<TNum>(TNum distance, int triangleIndex)
where TNum:INumberBase<TNum>
{
    public readonly TNum Distance = distance;
    public readonly int TriangleIndex = triangleIndex;

    [System.Diagnostics.Contracts.Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BvhHitInfo<TOther> To<TOther>()
        where TOther : unmanaged, IFloatingPointIeee754<TOther>
        => new(TOther.CreateTruncating(Distance), TriangleIndex);
}

