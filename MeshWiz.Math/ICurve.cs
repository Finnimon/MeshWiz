using System.Numerics;

namespace MeshWiz.Math;

public interface ICurve<TVector, TNum>
    where TVector :unmanaged, IVector<TVector, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    TVector Traverse(TNum t);
    bool IsClosed { get; }
    
}