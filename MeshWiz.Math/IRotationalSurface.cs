using System.Diagnostics.Contracts;
using System.Numerics;

namespace MeshWiz.Math;

public interface IRotationalSurface<TNum> : ISurface3<TNum>, IMathSurface<Vector3<TNum>,TNum>, IGeodesicProvider<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    [Pure] public IDiscreteCurve<Vector3<TNum>, TNum> SweepCurve { get; }
    [Pure] public Ray3<TNum> SweepAxis { get; }
}

/// <summary>
/// interface for operations on mathematically defined surfaces that would for example be very slow on Meshes
/// </summary>
public interface IMathSurface<TVector, TNum> : ISurface<TVector, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum> 
    where TVector : unmanaged, IFloatingVector<TVector, TNum>
{
    [Pure]
    public TVector NormalAt(TVector p);

    [Pure]
    public TVector ClampToSurface(TVector p);
}