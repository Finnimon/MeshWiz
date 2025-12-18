using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace MeshWiz.Math;

public readonly struct Ray<TVector, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
    where TVector : unmanaged, IVector<TVector, TNum>
{
    public readonly TVector Origin;
    public readonly TVector Direction;

    public Ray(TVector origin, TVector direction)
    {
        Origin = origin;
        Direction = direction;
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TVector ClosestPoint(TVector p)
    {
        var v = p - Origin;
        var ndir = Direction;
        var dotProduct = v.Dot(ndir);
        var alongVector = dotProduct * ndir;
        return Origin + alongVector;
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TNum DistanceTo(TVector p) => ClosestPoint(p).DistanceTo(p);
}