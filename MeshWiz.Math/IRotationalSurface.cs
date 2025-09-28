using System.Numerics;

namespace MeshWiz.Math;

public interface IRotationalSurface<TNum> where TNum : unmanaged,IFloatingPointIeee754<TNum>
{
    public IDiscreteCurve<Vector3<TNum>, TNum> SweepCurve { get; }
    public Ray3<TNum> SweepAxis { get; }
}