using System.Numerics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public static class CurveMath
{
    public static (LineIndexer[] Indices, TVector[] Vertices) Indicate<TVector, TNum>
        (IEnumerable<Line<TVector, TNum>> lines)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        where TVector : unmanaged, IFloatingVector<TVector, TNum>
    {
        List<LineIndexer> indices = [];
        List<TVector> vertices = [];
        Dictionary<TVector, int> unified = [];

        foreach (var line in lines)
        {
            var start = GetIndex(line.Start, unified, vertices);
            var end = GetIndex(line.End, unified, vertices);
            indices.Add(new LineIndexer(start, end));
        }

        return ([..indices], [..vertices]);
    }

    public static (LineIndexer[] Indices, TVector[] Vertices) Indicate<TVector, TNum>
        (IReadOnlyList<Line<TVector, TNum>> lines)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        where TVector : unmanaged, IFloatingVector<TVector, TNum>
    {
        var indices = new LineIndexer[lines.Count];
        var averageUniqueVertices = lines.Count / 2;
        var vertices = new List<TVector>(averageUniqueVertices);
        var unified = new Dictionary<TVector, int>(averageUniqueVertices);

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var start = GetIndex(line.Start, unified, vertices);
            var end = GetIndex(line.End, unified, vertices);
            indices[i] = new LineIndexer(start, end);
        }

        return (indices, [..vertices]);
    }

    private static int GetIndex<TElement>(TElement vec,
        Dictionary<TElement, int> unified,
        List<TElement> elements)
        where TElement : notnull
    {
        if (unified.TryGetValue(vec, out var index)) return index;
        index = elements.Count;
        unified.Add(vec, index);
        elements.Add(vec);
        return index;
    }

    public static PolyLine<TVec, TNum>[] Unify<TVec, TNum>(Queue<Line<TVec, TNum>> segments,
        TNum? squareTolerance = null)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        where TVec : unmanaged, IFloatingVector<TVec, TNum>
    {
        var epsilon = squareTolerance ?? TNum.CreateTruncating(0.00001);
        if (segments is { Count: 0 }) return [];
        List<PolyLine<TVec, TNum>> polyLines = [];
        LinkedList<Line<TVec, TNum>> connected = [];
        connected.AddLast(segments.Dequeue());
        var checkedSinceLastAdd = 0;
        while (segments.TryDequeue(out var line))
        {
            if (checkedSinceLastAdd > segments.Count)
            {
                polyLines.Add(PolyLine<TVec, TNum>.FromSegments(connected));
                connected = [];
                connected.AddLast(line);
                checkedSinceLastAdd = 0;
                continue;
            }

            var currentStart = connected.First!.Value.Start;
            var currentEnd = connected.Last!.Value.End;
            var checkedPrev = checkedSinceLastAdd;
            checkedSinceLastAdd = 0;
            if (currentStart.IsApprox(line.End, epsilon))
            {
                connected.AddFirst(line);
            }
            else if (currentEnd.IsApprox(line.Start, epsilon))
            {
                connected.AddLast(line);
            }
            else if (currentStart.IsApprox(line.Start, epsilon))
            {
                connected.AddFirst(line.Reversed);
            }
            else if (currentEnd.IsApprox(line.End, epsilon))
            {
                connected.AddLast(line.Reversed);
            }
            else
            {
                segments.Enqueue(line);
                checkedSinceLastAdd = checkedPrev + 1;
            }
        }

        if (connected.Count > 0)
        {
            polyLines.Add(PolyLine<TVec, TNum>.FromSegments(connected));
        }

        return polyLines.ToArray();
    }

    public static PolyLine<TVec, TNum>[] UnifyNonReversing<TVec, TNum>(RollingList<Line<TVec, TNum>> segments,
        TNum? squareTolerance = null)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        where TVec : unmanaged, IFloatingVector<TVec, TNum>
    {
        var epsilon = squareTolerance ?? CalculateMinimumEpsilon(segments);

        if (segments is { Count: 0 }) return [];
        if (segments is { Count: 1 }) return [new PolyLine<TVec, TNum>(segments[0].Start, segments[0].End)];

        List<PolyLine<TVec, TNum>> polyLines = [];
        RollingList<TVec> connected = new(0);
        var checkedSinceLastAdd = int.MaxValue;
        var frontDirection = TVec.NaN;
        var backDirection = TVec.NaN;
        while (segments.TryPopBack(out var line))
        {
            if (line.SquaredLength < epsilon)
                continue;

            if (checkedSinceLastAdd > segments.Count + 1)
            {
                AddIfValid(polyLines, connected, epsilon);
                connected = [line.Start, line.End];
                checkedSinceLastAdd = 0;
                backDirection = (frontDirection = line.NormalDirection);
                continue;
            }

            var connectedStart = connected[0];
            var connectedEnd = connected[^1];
            if (connectedStart.IsApprox(line.End, epsilon))
            {
                var newFrontDirection = line.NormalDirection;
                var sameDirection = newFrontDirection.Dot(frontDirection).IsApprox(TNum.One);
                if (sameDirection) connected[0] = line.Start;
                else connected.PushFront(line.Start);
                frontDirection = newFrontDirection;
                checkedSinceLastAdd = 0;
            }
            else if (connectedEnd.IsApprox(line.Start, epsilon))
            {
                var newBackDirection = line.NormalDirection;
                var sameDirection = newBackDirection.Dot(backDirection).IsApprox(TNum.One);
                if (sameDirection) connected[^1] = line.End;
                else connected.PushBack(line.End);
                backDirection = newBackDirection;
                checkedSinceLastAdd = 0;
            }
            else
            {
                segments.PushFront(line);
                checkedSinceLastAdd++;
            }
        }

        AddIfValid(polyLines, connected, epsilon);
        return polyLines.ToArray();
    }

    private static TNum CalculateMinimumEpsilon<TNum, TVec>(IReadOnlyList<Line<TVec, TNum>> segments)
        where TNum : unmanaged, IFloatingPointIeee754<TNum> where TVec : unmanaged, IFloatingVector<TVec, TNum>
    {
        var epsilon = TNum.CreateTruncating(float.MaxValue);
        foreach (var segment in segments)
        {
            var sqLength = segment.SquaredLength;
            if (sqLength <= TNum.Epsilon) continue;
            if (sqLength >= epsilon) continue;
            epsilon = sqLength;
        }

        return epsilon / TNum.CreateTruncating(2);
    }


    private static void AddIfValid<TVec, TNum>(List<PolyLine<TVec, TNum>> polyLines,
        RollingList<TVec> connected, TNum minLength)
        where TVec : unmanaged, IFloatingVector<TVec, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>

    {
        if (connected.Count < 2) return;

        var poly = new PolyLine<TVec, TNum>(connected.ToArrayFast());
        if (poly.Count < 1) return;
        var length = poly.Length;
        if (length < minLength) return;

        polyLines.Add(poly);
    }
}