using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public sealed class Polyline<TVector, TNum>(params TVector[] points)
    : IContiguousDiscreteCurve<TVector, TNum>, IReadOnlyList<Line<TVector, TNum>>
    where TVector : unmanaged, IFloatingVector<TVector, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember]
    private TVector? _vertexCentroid;
    
    private TVector[] _points=points;
    public ReadOnlySpan<TVector> Points => _points;
    

    private Polyline() : this(points: []) { }

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TVector Start => _points[0];

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TVector End => _points[^1];

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public int Count => int.Max(_points.Length - 1, 0);

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public static Polyline<TVector, TNum> Empty { get; } = [];

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember]
    private AABB<TVector>? _bbox;

    private TNum? _length;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public AABB<TVector> BBox => _bbox ??= AABB<TVector>.From(_points);

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public unsafe Line<TVector, TNum> this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (Count <= (uint)index) IndexThrowHelper.Throw(index, Count);
            return Unsafe.As<TVector,Line<TVector, TNum>>(ref _points[index]);
        }
    }

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TVector VertexCentroid => _vertexCentroid ??= GetVertexCentroid();

    private TVector GetVertexCentroid()
    {
        var centroid = TVector.Zero;

        if (Count <= 0)
            return centroid;

        var count = TNum.CreateTruncating(Count);
        if (IsClosed)
        {
            for (var i = 0; i < _points.Length - 1; i++) centroid += _points[i];
            return centroid / count;
        }

        for (var i = 1; i < _points.Length - 1; i++) centroid += _points[i];
        centroid *= Numbers<TNum>.Two;
        centroid += _points[0] + _points[^1];
        return centroid / Numbers<TNum>.Two / count;
    }


    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public bool IsClosed => Count > 2 && _points[0].IsApprox(_points[^1]);

    /// <inheritdoc />
    public Polyline<TVector, TNum> ToPolyline()
        => this;

    /// <inheritdoc />
    public Polyline<TVector, TNum> ToPolyline(PolylineTessellationParameter<TNum> tessellationParameter)
        => this;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TNum Length => _length ??= CalculateLength();

    [field: AllowNull, MaybeNull] internal TNum[] CumulativeDistances => field ??= CalculateCumulativeDistances();

    private TNum[] CalculateCumulativeDistances()
    {
        if (_points.Length == 0)
            return [];
        if (_points.Length == 1)
            return [TNum.Zero];
        var previousPos = TNum.Zero;
        var previous = _points[0];
        var positions = new TNum[_points.Length];

        for (var i = 1; i < _points.Length - 1; i++)
        {
            var current = _points[i];
            previousPos = current.DistanceTo(previous) + previousPos;
            previous = current;
            positions[i] = previousPos;
        }

        positions[^1] = TNum.One;
        return positions;
    }


    private TNum CalculateLength()
    {
        if (_points.Length < 2) return TNum.Zero;
        var length = TNum.Zero;
        var previous = _points[0];
        for (var i = 1; i < _points.Length; i++)
        {
            var current = _points[i];
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
        if (!fromStart && !fromEnd) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(by));
        TVector p1;
        TVector p2;
        if (fromEnd)
        {
            distance = Length - distance;
            p1 = _points[^1];
            p2 = _points[^2];
        }
        else
        {
            p1 = _points[0];
            p2 = _points[^1];
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
            return _points[0];
        if (by >= TNum.One)
            return _points[^1];

        var distance = by * Length;

        TryFindContainingSegmentExactly(distance, out var segment, out var remainder);
        var pStart = _points[segment];
        var pEnd = _points[segment + 1];
        return TVector.ExactLerp(pStart, pEnd, remainder);
    }


    public Polyline<TVector, TNum> this[TNum start, TNum end] => Section(start, end);

    public Polyline<TVector, TNum> Section(TNum start, TNum end) => ExactSection(Length * start, Length * end);

    public Polyline<TVector, TNum> ExactSection(TNum start, TNum end)
    {
        if (start.IsApprox(end)) return Empty;
        var foundStart = TryFindContainingSegmentExactly(start, out var startSeg, out var startRem);
        if (!foundStart) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(start), start, "Could not find");
        var foundEnd = TryFindContainingSegmentExactly(end, out var endSeg, out var endRem);
        if (!foundEnd) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(end), end, "Could not find");
        var wrappingAroundEnd = endSeg < startSeg || (endSeg == startSeg && endRem < startRem);
        if (wrappingAroundEnd && !IsClosed) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(end));
        TVector[] section;
        if (!wrappingAroundEnd)
        {
            if (startRem.IsApprox(this[startSeg].Length))
            {
                startSeg++;
                startRem = TNum.Zero;
            }

            if (endRem.IsApprox(TNum.Zero))
            {
                endSeg--;
                endRem = this[endSeg].Length;
            }

            var exlusivePIndex = endSeg + 2;
            section = _points[startSeg..exlusivePIndex];
        }
        else
        {
            var pSpan = _points.AsSpan();
            var firstChunk = pSpan[startSeg..];
            var secondChunk = pSpan[1..(endSeg + 2)];
            if (startRem.IsApprox(this[startSeg].Length))
            {
                if (firstChunk.Length != 0) firstChunk = firstChunk[^1..];
                startRem = TNum.Zero;
            }

            var dontTrimEnd = false;
            if (endRem.IsApprox(TNum.Zero))
            {
                secondChunk = secondChunk[..^1];
                dontTrimEnd = true;
            }

            section = new TVector[firstChunk.Length + secondChunk.Length];
            firstChunk.CopyTo(section);
            secondChunk.CopyTo(section.AsSpan(firstChunk.Length));
            if (dontTrimEnd) endRem = section[^2].DistanceTo(section[^1]);
        }

        Polyline<TVector, TNum> result = new(section);
        TrimEnds(result, startRem, endRem);
        return result;
    }

    private static void TrimEnds(Polyline<TVector, TNum> polyline, TNum atStartExactly, TNum atEndExactly)
    {
        polyline._points[0] = TVector.ExactLerp(polyline._points[0], polyline._points[1], atStartExactly);
        polyline._points[^1] = TVector.ExactLerp(polyline._points[^2], polyline._points[^1], atEndExactly);
    }


    [Pure]
    private bool TryFindContainingSegmentExactly(TNum distance, out int seg, out TNum remainder)
    {
        if (distance.IsApprox(TNum.Zero, TNum.Epsilon))
        {
            seg = 0;
            remainder = TNum.Zero;
            return true;
        }

        if (IsClosed) distance = distance.Wrap(TNum.Zero, Length);
        // Edge cases
        seg = -1;
        remainder = default;
        if (!AABB<TNum>.From(TNum.Zero, Length).Contains(distance)) return false;
        if (distance.IsApprox(TNum.Zero))
        {
            seg = 0;
            remainder = TNum.Zero;
        }
        else if (distance.IsApprox(Length))
        {
            seg = Count - 1;
            remainder = this[seg].Length;
        }


        var posBefore = 0;
        var posAfter = _points.Length - 1;

        // binary search for last index where CumulativeDistances[i] <= distance
        while (posAfter - posBefore > 1)
        {
            seg = (posBefore + posAfter) / 2;
            var midValue = CumulativeDistances[seg];

            if (midValue < distance)
                posBefore = seg; // move lower bound up
            else
                posAfter = seg; // move upper bound down
        }

        // distance into the segment
        remainder = distance - CumulativeDistances[posBefore];

        seg = posBefore;
        return true;
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
            var dot = curDirection.Dot(prevDirection);
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
        if (!(startDirection.Dot(endDirection)).IsApprox(TNum.One))
            return new Polyline<TVector, TNum>(points[..pCount]);
        points[0] = points[pI - 1];
        return new Polyline<TVector, TNum>(points[..pI]);
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
            var dot = curDirection.Dot(prevDirection);
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
        if (!(startDirection.Dot(endDirection)).IsApprox(TNum.One))
            return new Polyline<TVector, TNum>(points.ToArray());
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
            var dot = curDirection.Dot(prevDirection);
            var sameDirectionParallel = dot.IsApprox(TNum.One);

            if (sameDirectionParallel) points[^1] = line.End;
            else points.Add(line.End);

            prevDirection = curDirection;
        }

        return new Polyline<TVector, TNum>(points.ToArray());
    }

    public Polyline<TVector, TNum> CullDeadSegments(TNum? epsilon = null)
    {
        if (Count is 0)
            return Empty;
        if (Count is 1)
            return this[0].Length.IsApprox(TNum.Zero) ? Empty : this;
            
        epsilon ??= TNum.Epsilon;
        var p = _points[0];
        bool[]? toBeCulled = null;
        int cullCount = 0;
        for (var i = 1; i < _points.Length; i++)
        {
            var p2 = _points[i];
            if (!p.IsApprox(p2, epsilon.Value))
            {
                p = p2;
                continue;
            }

            cullCount++;
            toBeCulled ??= new bool[_points.Length];
            toBeCulled[i] = true;
        }

        if (cullCount == 0) return this;
        var culled = new TVector[_points.Length - cullCount];
        var culledPos = -1;
        for (var i = 0; i < _points.Length; i++)
        {
            if (toBeCulled![i]) continue;
            culled[++culledPos] = _points[i];
        }

        return new(culled);
    }

    [Pure]
    public Polyline<TVector, TNum> Shift(TVector shift)
    {
        Polyline<TVector, TNum> pl = new(_points.Select(p => p + shift).ToArray())
        {
            _vertexCentroid = _vertexCentroid is null ? null : VertexCentroid + shift,
            _length = TVector.IsFinite(shift) ? _length : null,
            _bbox = _bbox is null ? null : _bbox + shift
        };

        return pl;
    }

    public Polyline<TOtherVec, TOtherNum> To<TOtherVec, TOtherNum>()
        where TOtherVec : unmanaged, IFloatingVector<TOtherVec, TOtherNum>
        where TOtherNum : unmanaged, IFloatingPointIeee754<TOtherNum> =>
        new(_points.Select(TOtherVec.FromComponentsConstrained<TVector, TNum>).ToArray());

    /// <inheritdoc />
    public TVector GetTangent(TNum at)
    {
        var pos = at * Length;
        var found=TryFindContainingSegmentExactly(pos, out var seg, out _);
        return !found ? TVector.NaN : this[seg].NormalDirection;
    }

    /// <inheritdoc />
    public TVector EntryDirection => this[0].NormalDirection;

    /// <inheritdoc />
    public TVector ExitDirection =>this[^1].NormalDirection;
}