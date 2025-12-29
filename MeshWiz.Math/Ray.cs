using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace MeshWiz.Math;

public readonly struct Ray<TVec, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
    where TVec : unmanaged, IVec<TVec, TNum>
{
    public readonly TVec Origin;
    public readonly TVec Direction;

    public Ray(TVec origin, TVec direction)
    {
        Origin = origin;
        Direction = direction;
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TVec ClosestPoint(TVec p)
    {
        var v = p - Origin;
        var ndir = Direction;
        var dotProduct = v.Dot(ndir);
        var alongVector = dotProduct * ndir;
        return Origin + alongVector;
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum DistanceTo(TVec p) => ClosestPoint(p).DistanceTo(p);
}