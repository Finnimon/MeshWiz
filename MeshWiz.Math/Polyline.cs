using System.Collections;
using System.Numerics;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public sealed record Polyline<TVector, TNum>(params TVector[] Points)
    : IDiscreteCurve<TVector, TNum>, IReadOnlyList<Line<TVector, TNum>>
    where TVector : unmanaged, IFloatingVector<TVector, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    private TVector? _centroid;

    private Polyline() : this(Points: []) { }

    public TVector Start => Points[0];
    public TVector End => Points[^1];
    public int Count => Points.Length - 1;
    public static Polyline<TVector, TNum> Empty { get; } = [];

    public unsafe Line<TVector, TNum> this[int index]
    {
        get
        {
            if (Count > (uint)index)
                fixed (void* ptr = &Points[index])
                    return *(Line<TVector, TNum>*)ptr;
            throw new IndexOutOfRangeException();
        }
    }

    public TVector VertexCentroid => _centroid ??= GetVertexCentroid();

    private TVector GetVertexCentroid()
    {
        var centroid = TVector.Zero;

        if (Count <= 0)
            return centroid;

        var count = TNum.CreateTruncating(Count);
        if (IsClosed)
        {
            for (var i = 0; i < Points.Length - 1; i++) centroid += Points[i];
            return centroid / count;
        }

        foreach (var line in this) centroid += line.MidPoint;
        return centroid / count;
    }

    private bool? _isClosed;

    public bool IsClosed =>
        _isClosed ??= Points.Length > 1 && Points[0].IsApprox(Points[^1], TNum.CreateChecked(0.000001));

    private TNum? _length;
    public TNum Length => _length ??= CalculateLength();

    private TNum[]? _positions;
    private TNum[] CumulativeDistances => _positions ??= CalculatePositions();

    private TNum[] CalculatePositions()
    {
        if (Points.Length == 0)
            return [];
        if (Points.Length == 1)
            return [TNum.Zero];
        var previousPos = TNum.Zero;
        var previous = Points[0];
        var positions = new TNum[Points.Length];

        for (var i = 1; i < Points.Length - 1; i++)
        {
            var current = Points[i];
            previousPos = current.DistanceTo(previous) + previousPos;
            previous = current;
            positions[i] = previousPos;
        }

        positions[^1] = TNum.One;
        return positions;
    }


    private TNum CalculateLength()
    {
        if (Points.Length < 2) return TNum.Zero;
        var length = TNum.Zero;
        var previous = Points[0];
        for (var i = 1; i < Points.Length; i++)
        {
            var current = Points[i];
            length += previous.DistanceTo(current);
            previous = current;
        }

        return length;
    }


    public TVector Traverse(TNum distance)
        => (IsClosed || (TNum.Zero <= distance && distance <= TNum.One))
            ? TraverseOnCurve(distance)
            : TraverseFromEnds(distance);

    private TVector TraverseFromEnds(TNum by)
    {
        if (by == TNum.Zero) return Start;
        if (by == TNum.One) return End;

        var distance = by * Length;
        var fromStart = by < TNum.Zero;
        var fromEnd = by > TNum.One;
        if (!fromStart && !fromEnd) throw new ArgumentOutOfRangeException(nameof(by));
        TVector p1;
        TVector p2;
        if (fromEnd)
        {
            distance = Length - distance;
            p1 = Points[^1];
            p2 = Points[^2];
        }
        else
        {
            p1 = Points[0];
            p2 = Points[^1];
        }

        return TVector.ExactLerp(p1, p2, distance);
    }

    public TVector TraverseOnCurve(TNum by)
    {
        by = IsClosed
            ? by.Wrap(TNum.Zero, TNum.One)
            : TNum.Clamp(by, TNum.Zero, TNum.One);

        // Edge cases
        if (by <= TNum.Zero)
            return Points[0];
        if (by >= TNum.One)
            return Points[^1];

        var distance = by * Length;

        int posBefore = 0;
        int posAfter = Points.Length - 1;

        // Binary search for last index where CumulativeDistances[i] <= distance
        while (posAfter - posBefore > 1)
        {
            int mid = (posBefore + posAfter) / 2;
            var midValue = CumulativeDistances[mid];

            if (midValue.IsApprox(distance))
                return Points[mid];
            if (midValue < distance)
                posBefore = mid; // move lower bound up
            else
                posAfter = mid; // move upper bound down
        }

        // Distance into the segment
        distance -= CumulativeDistances[posBefore];

        var pStart = Points[posBefore];
        var pEnd = Points[posAfter];
        return TVector.ExactLerp(pStart, pEnd, distance);
    }


    /// <inheritdoc />
    public IEnumerator<Line<TVector, TNum>> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();


    public static Polyline<TVector, TNum> FromSegments(IReadOnlyList<Line<TVector, TNum>> list)
    {
        if (list.Count == 0) return Empty;
        if (list.Count == 1) return new([list[0].Start, list[0].End]);
        var points = new TVector[list.Count + 1];
        var firstLine = list[0];
        var prevDirection = firstLine.NormalDirection;
        var pI = -1;
        points[++pI] = firstLine.Start;
        points[++pI] = firstLine.End;
        for (var index = 1; index < list.Count; index++)
        {
            var line = list[index];
            var curDirection = line.NormalDirection;
            var dot = curDirection * prevDirection;
            var sameDirectionParallel = dot.IsApprox(TNum.One);

            if (sameDirectionParallel) points[pI] = line.End;
            else points[++pI] = line.End;

            prevDirection = curDirection;
        }

        var pCount = pI + 1;
        if (pCount == points.Length) return new(points);
        if (pCount < 4) return new Polyline<TVector, TNum>(points[..pCount]);
        var startPt = points[0];
        var endPt = points[pI];
        var isClosed = startPt.IsApprox(endPt);
        if (!isClosed) return new Polyline<TVector, TNum>(points[..pCount]);
        var startDirection = (points[1] - startPt).Normalized;
        var endDirection = (endPt - points[pI - 1]).Normalized;
        if (!(startDirection * endDirection).IsApprox(TNum.One)) return new Polyline<TVector, TNum>(points[..pCount]);
        points[0] = points[pI - 1];
        return new Polyline<TVector, TNum>(points[..(pI)]);
    }

    public static Polyline<TVector, TNum> FromSegments(IEnumerable<Line<TVector, TNum>> connected)
    {
        List<TVector> points = new();
        var prevDirection = TVector.NaN;
        var first = true;
        foreach (var line in connected)
        {
            if (first)
            {
                points.Add(line.Start);
                points.Add(line.End);
                prevDirection = line.NormalDirection;
                first = false;
                continue;
            }

            var curDirection = line.NormalDirection;
            var dot = curDirection * prevDirection;
            var sameDirectionParallel = dot.IsApprox(TNum.One);

            if (sameDirectionParallel) points[^1] = line.End;
            else points.Add(line.End);

            prevDirection = curDirection;
        }

        if (points.Count < 4) return new Polyline<TVector, TNum>(points.ToArray());
        var startPt = points[0];
        var endPt = points[^1];
        var areEqual = startPt.IsApprox(endPt);
        if (!areEqual) return new Polyline<TVector, TNum>(points.ToArray());
        var startDirection = (points[1] - startPt).Normalized;
        var endDirection = (endPt - points[^2]).Normalized;
        if (!(startDirection * endDirection).IsApprox(TNum.One)) return new Polyline<TVector, TNum>(points.ToArray());
        points[0] = points[^2];
        return new Polyline<TVector, TNum>(points.ToArray());
    }

    public static Polyline<TVector, TNum> FromSegmentCollection(IReadOnlyCollection<Line<TVector, TNum>> collection)
    {
        if (collection.Count == 0) return Empty;
        var firstLine = collection.First();
        if (collection.Count == 1) return new([firstLine.Start, firstLine.End]);

        List<TVector> points = new(collection.Count + 1);
        var prevDirection = firstLine.NormalDirection;
        points.Add(firstLine.Start);
        points.Add(firstLine.End);
        foreach (var line in collection)
        {
            var curDirection = line.NormalDirection;
            var dot = curDirection * prevDirection;
            var sameDirectionParallel = dot.IsApprox(TNum.One);

            if (sameDirectionParallel) points[^1] = line.End;
            else points.Add(line.End);

            prevDirection = curDirection;
        }

        return new Polyline<TVector, TNum>(points.ToArray());
    }
}