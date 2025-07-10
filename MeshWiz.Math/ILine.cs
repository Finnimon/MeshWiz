using System.Numerics;

namespace MeshWiz.Math;

public interface ILine<TVector, TNum>
    : IDiscreteCurve<TVector, TNum>
    where TVector :unmanaged, IFloatingVector<TVector, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    TVector Direction => End.Subtract(Start);
    TVector NormalDirection => Direction.Normalized;
    TNum IDiscreteCurve<TVector,TNum>.Length => Direction.Length;
    bool ICurve<TVector, TNum>.IsClosed => false;
    TVector MidPoint { get; }
}