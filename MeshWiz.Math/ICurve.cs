using System.Numerics;

namespace MeshWiz.Math;

public interface ICurve<out TVec, in TNum>
    where TVec :unmanaged, IVec<TVec, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    TVec Traverse(TNum t);
    bool IsClosed { get; }
    
}