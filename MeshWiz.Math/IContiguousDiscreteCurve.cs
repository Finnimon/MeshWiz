using System.Numerics;

namespace MeshWiz.Math;

public interface IContiguousDiscreteCurve<TVector, TNum> : IContiguousCurve<TVector, TNum>,
    IDiscreteCurve<TVector, TNum> 
    where TVector : unmanaged, IVector<TVector, TNum> 
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public TVector EntryDirection { get; }
    public TVector ExitDirection { get; }
}