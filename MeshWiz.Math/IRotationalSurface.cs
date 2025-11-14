using System.Diagnostics.Contracts;
using System.Numerics;

namespace MeshWiz.Math;

public interface IRotationalSurface<TNum> : ISurface3<TNum>, IMathSurface<Vector3<TNum>,TNum>, IGeodesicProvider<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    [Pure] public IDiscreteCurve<Vector3<TNum>, TNum> SweepCurve { get; }
    [Pure] public Ray3<TNum> SweepAxis { get; }
}