using System.Numerics;

namespace MeshWiz.Math;

public interface ILine<TVector, TNum>
    : IDiscreteCurve<TVector, TNum>
    where TVector : IFloatingVector<TVector, TNum>
    where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum>
{
    TVector Direction => End.Subtract(Start);
    TVector NormalDirection => Direction.Normalized;
    TNum IDiscreteCurve<TVector,TNum>.Length => Direction.Length;
    bool ICurve<TVector, TNum>.IsClosed => false;
}