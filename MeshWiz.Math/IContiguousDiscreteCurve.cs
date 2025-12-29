using System.Numerics;

namespace MeshWiz.Math;

public interface IContiguousDiscreteCurve<TVec, TNum> : IContiguousCurve<TVec, TNum>,
    IDiscreteCurve<TVec, TNum> 
    where TVec : unmanaged, IVec<TVec, TNum> 
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public TVec EntryDirection { get; }
    public TVec ExitDirection { get; }
}