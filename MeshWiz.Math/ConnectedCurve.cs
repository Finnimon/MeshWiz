using System.Numerics;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public sealed record ConnectedCurve<TVector,TNum>(IDiscreteCurve<TVector,TNum>[] Children) 
    : IDiscreteCurve<TVector,TNum>
    where TVector : IFloatingVector<TVector, TNum>
    where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum>
{
    
    private TNum? _length;
    public TNum Length =>_length??=Children.Aggregate(TNum.Zero, (length,child) => length+child.Length);
    public bool IsClosed => Start.Equals(End);
    public TVector Start =>Children[0].Start;
    public TVector End=>Children[^1].End;
    public TVector Traverse(TNum distance)
    {
        var onCurve = IsClosed||(TNum.Zero <= distance && distance <= Length);
        return onCurve? TraverseOnCurve(distance):TraverseOffOfCurve(distance);
    }

    private TVector TraverseOffOfCurve(TNum distance)
    {
        if (distance<=TNum.Zero) return Children[0].Traverse(distance);
        var lastChild = Children[^1];
        distance=distance-Length+lastChild.Length;
        return lastChild.Traverse(distance);
    }

    private TVector TraverseClosedReverse(TNum distance)
    {
        var rollingDistance=TNum.Clamp(distance, TNum.Zero, Length);
        for (var i = 0; i < Children.Length; i++)
        {
            var child = Children[i];
            var newDistance = rollingDistance + child.Length;
            if(newDistance>=TNum.Zero) return child.Traverse(newDistance);
            rollingDistance=newDistance;
        }
        throw new ArgumentOutOfRangeException(nameof(distance),distance,null);
    }

    public TVector TraverseOnCurve(TNum distance)
    {
        if(IsClosed) distance=distance.Wrap(TNum.Zero,Length);
        if(distance<TNum.Zero) return TraverseClosedReverse(distance);
        return TraverseOnCurveForward(distance);
    }

    private TVector TraverseOnCurveForward(TNum distance)
    {
        var rollingDistance=TNum.Clamp(distance, TNum.Zero, Length);
        for (var i = 0; i < Children.Length; i++)
        {
            var child = Children[i];
            var newDistance = rollingDistance - child.Length;
            if(newDistance<=TNum.Zero) return child.Traverse(rollingDistance);
            rollingDistance=newDistance;
        }
        throw new ArgumentOutOfRangeException(nameof(distance),distance,null);
    }
}