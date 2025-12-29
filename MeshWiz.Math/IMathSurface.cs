using System.Diagnostics.Contracts;
using System.Numerics;

namespace MeshWiz.Math;

/// <summary>
/// interface for operations on mathematically defined surfaces that would for example be very slow on Meshes
/// </summary>
public interface IMathSurface<TVec, out TNum> : ISurface<TVec, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum> 
    where TVec : unmanaged, IVec<TVec, TNum>
{
    [Pure]
    public TVec NormalAt(TVec p);

    [Pure]
    public TVec ClampToSurface(TVec p);
}