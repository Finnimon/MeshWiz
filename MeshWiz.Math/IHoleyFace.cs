using System.Numerics;

namespace MeshWiz.Math;

public interface IHoleyFace<TVector, TNum>
    where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum>
    where TVector : unmanaged, IFloatingVector<TVector, TNum>
{
    ICurve<TVector, TNum>[] InnerBounds { get; }
}