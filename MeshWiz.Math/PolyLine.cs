using System.Collections;
using System.Numerics;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public sealed record PolyLine<TVector, TNum>(TVector[] Points) 
    : IDiscreteCurve<TVector, TNum>, IReadOnlyList<Line<TVector, TNum>>
    where TVector : unmanaged, IFloatingVector<TVector, TNum>
    where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum>
{
    public TVector Start => Points[0];
    public TVector End => Points[^1];
    public int Count => Points.Length-1;

    public Line<TVector, TNum> this[int index]
    {
        get
        {
            if (index.InsideInclusiveRange(0, Count))
                return new Line<TVector, TNum>(Points[index], Points[index + 1]);
            throw new IndexOutOfRangeException();
        }
    }

    public bool IsClosed => End.Equals(Start);

    public TNum Length
    {
        get
        {
            var length = TNum.Zero;
            var start = Start;
            for (var i = 1; i < Count; i++)
            {
                var current = Points[i];
                length += start.Distance(in current);
                start = current;
            }

            return length;
        }
    }

    public TVector Traverse(TNum distance)
        => (IsClosed || (TNum.Zero <= distance && distance <= Length))
            ? TraverseOnCurve(distance)
            : TraverseFromEnds(distance);

    private TVector TraverseFromEnds(TNum distance)
    {
        var reverse = distance < TNum.Zero;
        var end = reverse ? this[0] : this[^1];
        distance = reverse ? distance : distance - Length + end.Length;
        return end.Traverse(distance);
    }

    public TVector TraverseOnCurve(TNum distance)
    {
        if (!IsClosed) distance = TNum.Clamp(distance, TNum.Zero, Length);
        distance = distance.Wrap(TNum.Zero, Length);
        return distance >= TNum.Zero
            ? TraverseOnCurveForward(distance)
            : TraverseOnCurveReverse(distance);
    }

    private TVector TraverseOnCurveReverse(TNum distance)
    {
        var rollingDistance = distance;
        for (var i = this.Count - 1; i >= 0; i--)
        {
            var line = this[i];
            rollingDistance += line.Length;
            if (rollingDistance >= TNum.Zero)
                return line.Traverse(rollingDistance);
        }

        throw new ArgumentOutOfRangeException(nameof(distance));
    }

    private TVector TraverseOnCurveForward(TNum distance)
    {
        var rollingDistance = distance;
        for (var i = 0; i < this.Count; i++)
        {
            var line = this[i];
            var nextDistance = rollingDistance - line.Length;
            if (nextDistance <= TNum.Zero) return line.Traverse(rollingDistance);
            rollingDistance = nextDistance;
        }

        throw new ArgumentOutOfRangeException(nameof(distance));
    }

    public IEnumerator<Line<TVector, TNum>> GetEnumerator()
    {
        var count = Count;
        for (var i = 0; i < count; i++)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}