using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Line<TVector, TNum>(TVector start, TVector end)
    : ILine<TVector, TNum>
    where TVector : unmanaged, IFloatingVector<TVector, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    private readonly TVector _start=start;
    private readonly TVector _end=end;
    public TVector Start => _start;
    public TVector End => _end;
    public TVector MidPoint => (_start + _end)/TNum.CreateTruncating(2);
    bool ICurve<TVector, TNum>.IsClosed => false;
    public TNum Length => Direction.Length;
    public TVector Direction => _end.Subtract(_start);
    public TVector NormalDirection => Direction.Normalized;
    public Line<TVector, TNum> Clone() => new(_start, _end);
    public Line<TVector, TNum> Reversed => new(_end,_start);
    TNum IDiscreteCurve<TVector,TNum>.Length => Direction.Length;
    
    
    public static Line<TVector,TNum> FromDirection(TVector start,TVector direction)
        => new(start, start.Add(direction));
    
    
    public TVector Traverse(TNum distance)
        =>_start+Direction*distance;

    public TVector TraverseOnCurve(TNum distance) 
        => Traverse(TNum.Clamp(distance, TNum.Zero, TNum.One));

}