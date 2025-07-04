using System.Numerics;

namespace MeshWiz.Math;

public interface ICurve<TVector, TNum>
    where TVector :unmanaged, IFloatingVector<TVector, TNum>
    where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum>
{
    TVector Traverse(TNum distance);
    bool IsClosed { get; }
}