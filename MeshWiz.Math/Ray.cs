using System.Numerics;

namespace MeshWiz.Math;

public readonly struct Ray<TVector,TNum> 
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
    where TVector: unmanaged, IVector<TVector,TNum>
{
    public readonly TVector Origin;
    public readonly TVector Direction;
    public Ray(TVector origin, TVector direction)
    {
        Origin = origin;
        Direction = direction;
    }
}