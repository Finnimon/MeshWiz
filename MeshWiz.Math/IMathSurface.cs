using System.Diagnostics.Contracts;
using System.Numerics;

namespace MeshWiz.Math;

/// <summary>
/// interface for operations on mathematically defined surfaces that would for example be very slow on Meshes
/// </summary>
public interface IMathSurface<TVector, TNum> : ISurface<TVector, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum> 
    where TVector : unmanaged, IVector<TVector, TNum>
{
    [Pure]
    public TVector NormalAt(TVector p);

    [Pure]
    public TVector ClampToSurface(TVector p);
}