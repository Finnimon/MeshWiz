using System.Collections;
using System.Drawing;
using System.Numerics;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public sealed record PolyLine<TVector, TNum>(TVector[] Points)
    : IDiscreteCurve<TVector, TNum>, IReadOnlyList<Line<TVector, TNum>>
    where TVector : unmanaged, IFloatingVector<TVector, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    private TVector? _centroid;
    public TVector Start => Points[0];
    public TVector End => Points[^1];
    public int Count => Points.Length - 1;
    public static PolyLine<TVector, TNum> Empty { get; } = new([]);

    public Line<TVector, TNum> this[int index]
    {
        get
        {
            if (index.InsideInclusiveRange(0, Count - 1))
                return new Line<TVector, TNum>(Points[index], Points[index + 1]);
            throw new IndexOutOfRangeException();
        }
    }

    public TVector Centroid => _centroid ??= GetCentroid();

    private TVector GetCentroid()
    {
        var centroid = TVector.Zero;

        if (Count <= 0)
        {
            return centroid;
        }

        if (IsClosed)
        {
            for (var i = 0; i < Points.Length-1; i++) centroid += Points[i];
            return centroid / TNum.CreateTruncating(Count);
        }

        for (var i = 1; i < Points.Length - 1; i++) centroid += Points[i];
        centroid*=TNum.CreateTruncating(2);        
        centroid += Points[0];
        centroid += Points[^1];
        var divisor=TNum.CreateTruncating(2*Count-2);
        return centroid / divisor;
    }

    public bool IsClosed => Count > 0 && Points[0] == Points[^1];

    public TNum Length
    {
        get
        {
            var length = TNum.Zero;
            var start = Start;
            for (var i = 1; i < Count; i++)
            {
                var current = Points[i];
                length += start.Distance(current);
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