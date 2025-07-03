using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Line<TVector, TNum>(TVector Start, TVector End)
    : ILine<TVector, TNum>
    where TVector : unmanaged, IFloatingVector<TVector, TNum>
    where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum>
{
    bool ICurve<TVector, TNum>.IsClosed => false;
    public TNum Length => Direction.Length;
    public TVector Direction => End.Subtract(Start);
    public TVector NormalDirection => Direction.Normalized;
    TNum IDiscreteCurve<TVector,TNum>.Length => Direction.Length;
    
    
    public static Line<TVector,TNum> FromDirection(TVector start,TVector direction)
        => new(start, start.Add(direction));
    
    
    public TVector Traverse(TNum distance)
        =>NormalDirection.Scale(scalar: distance).Add(Start);

    public TVector TraverseOnCurve(TNum distance) 
        => Traverse(TNum.Clamp(distance, TNum.Zero, Length));
}