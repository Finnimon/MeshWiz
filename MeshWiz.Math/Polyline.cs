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

public sealed class Polyline<TVec, TNum> 
    : IPolyline<Polyline<TVec,TNum>,Line<TVec,TNum>,TVec,TVec,TNum>
    where TVec : unmanaged, IVec<TVec, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember]
    private TVec? _vertexCentroid;

    private readonly TVec[] _points;
    public ReadOnlySpan<TVec> Points => _points;


    public Polyline(params ReadOnlySpan<TVec> points) => _points = points.ToArray();
    public Polyline(IEnumerable<TVec> pts) : this(pts.ToArray()) { }
    internal Polyline(TVec[] points) => _points = points;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TVec Start => _points[0];

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TVec End => _points[^1];

    /// <inheritdoc />
    public bool Contains(Line<TVec, TNum> item)
        => IndexOf(item) != -1;

    /// <inheritdoc />
    public void CopyTo(Line<TVec, TNum>[] array, int arrayIndex)
    {
        for (var i = 0; i < this.Count; i++) array[arrayIndex + i] = this[i];
    }

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public int Count => int.Max(_points.Length - 1, 0);

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public static Polyline<TVec, TNum> Empty { get; } = [];

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember]
    private AABB<TVec>? _bbox;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public AABB<TVec> BBox => _bbox ??= AABB<TVec>.From(_points);

    /// <inheritdoc />
    public int IndexOf(Line<TVec, TNum> item)
    {
        var i = -1;
        var count = Count;
        while (++i < count && this[i] != item) ;
        return i == count ? -1 : i;
    }

    /// <inheritdoc />
    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public Line<TVec, TNum> this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (Count <= (uint)index) IndexThrowHelper.Throw(index, Count);
            return Unsafe.As<TVec, Line<TVec, TNum>>(ref _points[index]);
        }
    }

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TVec VertexCentroid => _vertexCentroid ??= GetVertexCentroid();

    private TVec GetVertexCentroid()
    {
        var centroid = TVec.Zero;

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
    public Polyline<TVec, TNum> ToPolyline()
        => this;

    /// <inheritdoc />
    public Polyline<TVec, TNum> ToPolyline(PolylineTessellationParameter<TNum> tessellationParameter)
        => this;

    [JsonIgnore, XmlIgnore, SoapIgnore, IgnoreDataMember, Pure]
    public TNum Length => _points.Length > 1 ? CumulativeDistances[^1] : TNum.Zero;

    [field:AllowNull,MaybeNull]
    // ReSharper disable once InconsistentNaming
    private TNum[] _cumulativeDistances =>
        field ??= Polyline.CalculateCumulativeDistances<TVec, TNum>(verts: _points);

    public ReadOnlySpan<TNum> CumulativeDistances =>_cumulativeDistances;


    public TVec Traverse(TNum t)
        => Polyline.Traverse(t, CumulativeDistances, IsClosed, _points);

    public TVec TraverseOnCurve(TNum t)
        => Polyline.TraverseOnCurve(t, CumulativeDistances, IsClosed, _points);


    [Pure]
    public Polyline<TVec, TNum> this[TNum start, TNum end] => Section(start, end);
    [Pure]
    public Polyline<TVec, TNum> Section(TNum start, TNum end) => ExactSection(Length * start, Length * end);

    [Pure]
    public Polyline<TVec, TNum> ExactSection(TNum start, TNum end)
        => new(Polyline.ExactSection(start, end, _points, IsClosed, CumulativeDistances));


    /// <inheritdoc />
    public int Version { get; }

    /// <inheritdoc />
    public IEnumerator<Line<TVec, TNum>> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();


    public static Polyline<TVec, TNum> FromSegments(IReadOnlyList<Line<TVec, TNum>> list)
    {
        if (list.Count == 0) return Empty;
        if (list.Count == 1) return new([list[0].Start, list[0].End]);
        var points = new TVec[list.Count + 1];
        var firstLine = list[0];
        var prevDirection = firstLine.Direction;
        var pI = -1;
        points[++pI] = firstLine.Start;
        points[++pI] = firstLine.End;
        for (var index = 1; index < list.Count; index++)
        {
            var line = list[index];
            var curDirection = line.Direction;
            var dot = curDirection.Dot(prevDirection);
            var sameDirectionParallel = dot.IsApprox(TNum.One);

            if (sameDirectionParallel) points[pI] = line.End;
            else points[++pI] = line.End;

            prevDirection = curDirection;
        }

        var pCount = pI + 1;
        if (pCount == points.Length) return new(points);
        if (pCount < 4) return new Polyline<TVec, TNum>(points[..pCount]);
        var startPt = points[0];
        var endPt = points[pI];
        var isClosed = startPt.IsApprox(endPt);
        if (!isClosed) return new Polyline<TVec, TNum>(points[..pCount]);
        var startDirection = (points[1] - startPt).Normalized();
        var endDirection = (endPt - points[pI - 1]).Normalized();
        if (!startDirection.Dot(endDirection).IsApprox(TNum.One))
            return new Polyline<TVec, TNum>(points[..pCount]);
        points[0] = points[pI - 1];
        return new Polyline<TVec, TNum>(points[..pI]);
    }

    public static Polyline<TVec, TNum> FromSegments(IEnumerable<Line<TVec, TNum>> connected)
    {
        List<TVec> points = new();
        var prevDirection = TVec.NaN;
        var first = true;
        foreach (var line in connected)
        {
            if (first)
            {
                points.Add(line.Start);
                points.Add(line.End);
                prevDirection = line.Direction;
                first = false;
                continue;
            }

            var curDirection = line.Direction;
            var dot = curDirection.Dot(prevDirection);
            var sameDirectionParallel = dot.IsApprox(TNum.One);

            if (sameDirectionParallel) points[^1] = line.End;
            else points.Add(line.End);

            prevDirection = curDirection;
        }

        if (points.Count < 4) return new Polyline<TVec, TNum>(points.ToArray());
        var startPt = points[0];
        var endPt = points[^1];
        var areEqual = startPt.IsApprox(endPt);
        if (!areEqual) return new Polyline<TVec, TNum>(points.ToArray());
        var startDirection = (points[1] - startPt).Normalized();
        var endDirection = (endPt - points[^2]).Normalized();
        if (!(startDirection.Dot(endDirection)).IsApprox(TNum.One))
            return new Polyline<TVec, TNum>(points.ToArray());
        points[0] = points[^2];
        return new Polyline<TVec, TNum>(points.ToArray());
    }

    public static Polyline<TVec, TNum> FromSegmentCollection(IReadOnlyCollection<Line<TVec, TNum>> collection)
    {
        if (collection.Count == 0) return Empty;
        var firstLine = collection.First();
        if (collection.Count == 1) return new([firstLine.Start, firstLine.End]);

        List<TVec> points = new(collection.Count + 1);
        var prevDirection = firstLine.Direction;
        points.Add(firstLine.Start);
        points.Add(firstLine.End);
        foreach (var line in collection)
        {
            var curDirection = line.Direction;
            var dot = curDirection.Dot(prevDirection);
            var sameDirectionParallel = dot.IsApprox(TNum.One);

            if (sameDirectionParallel) points[^1] = line.End;
            else points.Add(line.End);

            prevDirection = curDirection;
        }

        return new Polyline<TVec, TNum>(points.ToArray());
    }

    public Polyline<TVec, TNum> CullDeadSegments(TNum? epsilon = null)
    {
        if (Count is 0)
            return Empty;
        if (Count is 1)
            return this[0].Length.IsApprox(TNum.Zero) ? Empty : this;

        epsilon ??= Numbers<TNum>.ZeroEpsilon;
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
        var culled = new TVec[_points.Length - cullCount];
        var culledPos = -1;
        for (var i = 0; i < _points.Length; i++)
        {
            if (toBeCulled![i]) continue;
            culled[++culledPos] = _points[i];
        }

        return new Polyline<TVec, TNum>(culled);
    }

    [Pure]
    public Polyline<TVec, TNum> Shift(TVec shift)
    {
        Polyline<TVec, TNum> pl = new(_points.Select(p => p + shift).ToArray())
        {
            _vertexCentroid = _vertexCentroid is null ? null : VertexCentroid + shift,
            _bbox = _bbox is null ? null : _bbox + shift
        };

        return pl;
    }

    public Polyline<TOtherVec, TOtherNum> To<TOtherVec, TOtherNum>()
        where TOtherVec : unmanaged, IVec<TOtherVec, TOtherNum>
        where TOtherNum : unmanaged, IFloatingPointIeee754<TOtherNum> =>
        new(_points.Select(TOtherVec.FromComponentsConstrained<TVec, TNum>).ToArray());

    /// <inheritdoc />
    public TVec GetTangent(TNum t)
    {
        var pos = t * Length;

        var found = Polyline.TryFindContainingSegmentExactly<TVec, TNum>(IsClosed,
            CumulativeDistances,
            pos,
            out var seg,
            out _);
        return !found ? TVec.NaN : this[seg].Direction;
    }

    /// <inheritdoc />
    public TVec EntryDirection => this[0].Direction;

    /// <inheritdoc />
    public TVec ExitDirection => this[^1].Direction;

    public static Polyline<TVec, TNum> CreateCulled(params ReadOnlySpan<TVec> poses)
    {
        return poses.Length is 0 or 1 ? Empty : CreateCulledNonCopying(poses.ToArray());
    }
    public static Polyline<TVec, TNum> CreateCulledNonCopying(TVec[] verts)
    {
        if (verts.Length is 0 or 1)
            return Empty;
        var vertCount = 0;
        for (var i = 0; i < verts.Length; ++i)
        {
            if (i == 0)
            {
                vertCount++;
                continue;
            }

            ref var previous = ref verts[vertCount - 1];
            var current = verts[i];
            var dist = previous.DistanceTo(current);
            var cull = dist.IsApproxZero();
            if (cull)
            {
                previous = TVec.Lerp(previous, current, Numbers<TNum>.Half);
                continue;
            }

            vertCount++;
            var noChange = i == vertCount;
            if (noChange) continue;
            verts[vertCount - 1] = current;
        }

        return new Polyline<TVec, TNum>(verts);
    }

    /// <inheritdoc />
    public IReadOnlyList<TVec> Vertices => _points;

    /// <inheritdoc />
    public IReadOnlyList<TNum> CumulativeLengths => _cumulativeDistances;

    /// <inheritdoc />
    public static Polyline<TVec, TNum> CreateNonCopying(TVec[] vertices) => new(vertices);

    /// <inheritdoc />
    public static Polyline<TVec, TNum> Create(IEnumerable<TVec> verts)
    =>new(verts.ToArray());

    /// <inheritdoc />
    public static Polyline<TVec, TNum> Create(params ReadOnlySpan<TVec> vertices) => new(vertices);

    /// <inheritdoc />
    public static Polyline<TVec, TNum> CreateCulled(IEnumerable<TVec> source) 
        => CreateNonCopying(Polyline.Cull<TVec, TNum>(source));
}

public static partial class Polyline
{
    public static TLerp Traverse<TLerp, TNum>(TNum distance, ReadOnlySpan<TNum> cumulativeDistances, bool isClosed,
        IReadOnlyList<TLerp> verts)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        where TLerp : ILerp<TLerp, TNum>
        => (isClosed || (TNum.Zero <= distance && distance <= TNum.One))
            ? TraverseOnCurve(distance, cumulativeDistances, isClosed, verts)
            : TraverseFromEnds(distance, cumulativeDistances, verts);

    private static TLerp TraverseFromEnds<TLerp, TNum>(TNum by, ReadOnlySpan<TNum> cumulateDistances,
        IReadOnlyList<TLerp> verts)
        where TLerp : ILerp<TLerp, TNum> where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        if (by == TNum.Zero) return verts[0];
        if (by == TNum.One) return verts[^1];
        var len = cumulateDistances[^1];
        var distance = by * len;
        var fromStart = by < TNum.Zero;
        var fromEnd = by > TNum.One;
        if (!fromStart && !fromEnd) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(by));
        TLerp p1;
        TLerp p2;
        if (fromEnd)
        {
            distance = len - distance;
            p1 = verts[^1];
            p2 = verts[^2];
        }
        else
        {
            p1 = verts[0];
            p2 = verts[^1];
        }

        return TLerp.ExactLerp(p1, p2, distance);
    }

    public static TLerp TraverseOnCurve<TLerp, TNum>(TNum t,
        ReadOnlySpan<TNum> cumulativeDistances,
        bool isClosed,
        IReadOnlyList<TLerp> verts)
        where TLerp : ILerp<TLerp, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        t = isClosed
            ? t.Wrap(TNum.Zero, TNum.One)
            : TNum.Clamp(t, TNum.Zero, TNum.One);

        // Edge cases
        if (t <= TNum.Zero)
            return verts[0];
        if (t >= TNum.One)
            return verts[^1];
        var len = cumulativeDistances[^1] - cumulativeDistances[0];
        var distance = t * len + cumulativeDistances[0];
        var found = TryFindContainingSegmentExactly<TLerp, TNum>(isClosed, cumulativeDistances, distance,
            out var segment,
            out var remainder);
        if (!found) ThrowHelper.ThrowInvalidOperationException();
        var pStart = verts[segment];
        var pEnd = verts[segment + 1];
        return TLerp.ExactLerp(pStart, pEnd, remainder);
    }

    [Pure]
    public static TNum[] CalculateCumulativeDistances<TPos, TNum>(ReadOnlySpan<TPos> verts)
        where TPos : IDistance<TPos, TNum>
        where TNum : INumber<TNum>
    {
        if (verts.Length == 0)
            return [];
        if (verts.Length == 1)
            return [TNum.Zero];
        var previousPos = TNum.Zero;
        var previous = verts[0];
        var positions = new TNum[verts.Length];
        positions[0] = previousPos;

        for (var i = 1; i < verts.Length; i++)
        {
            var current = verts[i];
            previousPos = current.DistanceTo(previous) + previousPos;
            previous = current;
            positions[i] = previousPos;
        }

        return positions;
    }

    [Pure]
    public static bool TryFindContainingSegmentExactly<TPos, TNum>(bool isClosed,
        ReadOnlySpan<TNum> cumulativeDistances,
        TNum distance, out int seg, out TNum remainder)
        where TPos : IDistance<TPos, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        if (distance.IsApproxZero())
        {
            seg = 0;
            remainder = TNum.Zero;
            return true;
        }

        var len = cumulativeDistances[^1];
        var count = cumulativeDistances.Length - 1;
        if (isClosed) distance = distance.Wrap(TNum.Zero, len);
        // Edge cases
        seg = -1;
        remainder = default;
        if (!AABB<TNum>.From(TNum.Zero, len).Contains(distance)) return false;
        if (distance.IsApprox(TNum.Zero))
        {
            seg = 0;
            remainder = TNum.Zero;
        }
        else if (distance.IsApprox(len))
        {
            seg = count - 1;
            remainder = cumulativeDistances[seg + 1] - cumulativeDistances[seg];
        }


        var posBefore = 0;
        var posAfter = count;

        // binary search for last index where CumulativeDistances[i] <= distance
        while (posAfter - posBefore > 1)
        {
            seg = (posBefore + posAfter) / 2;
            var midValue = cumulativeDistances[seg];

            if (midValue < distance)
                posBefore = seg; // move lower bound up
            else
                posAfter = seg; // move upper bound down
        }

        // distance into the segment
        remainder = distance - cumulativeDistances[posBefore];

        seg = posBefore;
        return true;
    }

    public static TLerp[] ExactSection<TLerp, TNum>(TNum start, TNum end, TLerp[] verts, bool isClosed,
        ReadOnlySpan<TNum> cumulativeDistances)
        where TLerp : ILerp<TLerp, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        if (start.IsApprox(end)) return [];
        var foundStart = TryFindContainingSegmentExactly<TLerp, TNum>(isClosed, cumulativeDistances, start,
            out var startSeg, out var startRem);
        if (!foundStart) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(start), start, "Could not find");
        var foundEnd =
            TryFindContainingSegmentExactly<TLerp, TNum>(isClosed, cumulativeDistances, end, out var endSeg,
                out var endRem);
        if (!foundEnd) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(end), end, "Could not find");
        var wrappingAroundEnd = endSeg < startSeg || (endSeg == startSeg && endRem < startRem);
        if (wrappingAroundEnd && !isClosed) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(end));
        TLerp[] section;
        if (!wrappingAroundEnd)
        {
            if (startRem.IsApprox(SegLength(cumulativeDistances, startSeg)))
            {
                startSeg++;
                startRem = TNum.Zero;
            }

            if (endRem.IsApprox(TNum.Zero))
            {
                endSeg--;
                endRem = SegLength(cumulativeDistances, endSeg);
            }

            var exclusivePIndex = endSeg + 2;
            section = verts[startSeg..exclusivePIndex];
        }
        else
        {
            var pSpan = verts.AsSpan();
            var firstChunk = pSpan[startSeg..];
            var secondChunk = pSpan[1..(endSeg + 2)];
            if (startRem.IsApprox(SegLength(cumulativeDistances, startSeg)))
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

            section = new TLerp[firstChunk.Length + secondChunk.Length];
            firstChunk.CopyTo(section);
            secondChunk.CopyTo(section.AsSpan(firstChunk.Length));
            if (dontTrimEnd) endRem = section[^2].DistanceTo(section[^1]);
        }


        TrimEnds(section, startRem, endRem);
        return section;
    }

    private static TNum SegLength<TNum>(ReadOnlySpan<TNum> distances, int seg)
        where TNum : INumber<TNum>
        => distances[seg + 1] - distances[seg];

    private static void TrimEnds<TLerp, TNum>(TLerp[] verts, TNum atStartExactly, TNum atEndExactly)
        where TLerp : ILerp<TLerp, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        if (atStartExactly.IsApprox(verts[0].DistanceTo(verts[1])))
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(atStartExactly), atStartExactly, null);
        if (atEndExactly.IsApprox(TNum.Zero))
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(atEndExactly), atEndExactly, null);

        verts[0] = TLerp.ExactLerp(verts[0], verts[1], atStartExactly);
        verts[^1] = TLerp.ExactLerp(verts[^2], verts[^1], atEndExactly);
    }

    

    public static TLerp[] Cull<TLerp, TNum>(IEnumerable<TLerp> source)
        where TLerp : ILerp<TLerp, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        
        var verts = source.ToArray();
        if (verts.Length < 2)
            return [];
        var tail = 0;
        var previous = verts[tail];
        var half = Numbers<TNum>.Half;
        for (var i = 1; i < verts.Length; i++)
        {
            var cur = verts[i];
            var dist = TLerp.Distance(previous, cur);
            var cull = dist.IsApproxZero();
            if (cull)
            {
                previous = verts[tail] = TLerp.Lerp(previous, cur, half);
            }
            else
            {
                tail++;
                previous = verts[tail] = verts[i];
            }
        }

        var count = tail + 1;
        if(count != verts.Length)
            Array.Resize(ref verts, count);
        return verts;
    }
    

    public static TLerp[] Cull<TLerp, TNum>(ReadOnlySpan<TLerp> source)
        where TLerp : ILerp<TLerp, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        
        if (source.Length < 2)
            return [];
        var tail = 0;
        var previous = source[tail];
        var half = Numbers<TNum>.Half;
        var culledBefore = false;
        TLerp[]? result = null;
        for (var i = 1; i < source.Length; i++)
        {
            var cur = source[i];
            var dist = TLerp.Distance(previous, cur);
            var cull = dist.IsApproxZero();
            if (!cull)
            {
                tail++;
                previous = culledBefore 
                    ? result![tail] = cur 
                    : cur;
                continue;
            }

            if (!culledBefore)
            {
                culledBefore = true;
                result = source.ToArray();
            }
            previous = result![tail] = TLerp.Lerp(previous, cur, half);
        }

        if (!culledBefore)
            return source.ToArray();
        
        var count = tail + 1;
        if(count != result!.Length)
            Array.Resize(ref result, count);
        return result;
    }

    public static Result<Arithmetics, TLerp[]> ForceConcat<TLerp, TNum>(params IEnumerable<IList<TLerp>> segs)
        where TLerp : ILerp<TLerp, TNum>
        where TNum : IFloatingPointIeee754<TNum>
    {
        var half = Numbers<TNum>.Half;
        TLerp[] result = [];
        var once = Bool.Once();
        var count = 0;
        foreach (var seg in segs)
        {
            if (seg.Count is 0 or 1)
                continue;
            if (once)
            {
                result = [..seg];
                count = seg.Count;
                continue;
            }

            count += seg.Count - 1;
            if (result.Length < seg.Count - 1 + count)
                Array.Resize(ref result, int.Max(result.Length + seg.Count - 1, result.Length * 2));
            var tail = count - 1;
            var last = result[tail];
            last = TLerp.Lerp(last, seg[0], half);
            seg.CopyTo(result, tail);
            result[tail] = last;
        }

        if (count is 0)
            return Result<Arithmetics, TLerp[]>.Failure(Arithmetics.Empty);
        Array.Resize(ref result, count);
        return result;
    }

    public static Result<Arithmetics, TLerp[]> ForceConcat<TLerp, TNum>(params IEnumerable<TLerp[]> segs)
        where TLerp : ILerp<TLerp, TNum>
        where TNum : IFloatingPointIeee754<TNum>
    {
        var half = Numbers<TNum>.Half;
        TLerp[] result = [];
        var once = Bool.Once();
        var count = 0;
        foreach (var seg in segs)
        {
            if (seg.Length is 0 or 1)
                continue;
            if (once)
            {
                result = [..seg];
                count = seg.Length;
                continue;
            }

            count += seg.Length - 1;
            if (result.Length < seg.Length - 1 + count)
                Array.Resize(ref result, int.Max(result.Length + seg.Length - 1, result.Length * 2));
            var tail = count - 1;
            var last = result[tail];
            last = TLerp.Lerp(last, seg[0], half);
            seg.CopyTo(result, tail);
            result[tail] = last;
        }

        if (count is 0)
            return Result<Arithmetics, TLerp[]>.Failure(Arithmetics.Empty);
        Array.Resize(ref result, count);
        return result;
    }
    
    
    public static Result<Arithmetics, TVector[]> ForceConcat<TVector, TNum>(params IEnumerable<Polyline<TVector,TNum>> segs)
        where TVector : unmanaged, IVec<TVector, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var half = Numbers<TNum>.Half;
        List<TVector> result = [];
        foreach (var polyline in segs)
        {
            var curPts = polyline.Points;
            if (curPts.Length is 0 or 1)
                continue;
            if (result.Count != 0)
                curPts = curPts[1..];
            result.AddRange(curPts);
        }

        return result.ToArray();
    }
    
    
    public static Result<Arithmetics, TPose[]> ForceConcat<TPose,TVector, TNum>(params IEnumerable<PosePolyline<TPose,TVector,TNum>> segs)
        where TVector : unmanaged, IVec<TVector, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        where TPose : IPose<TPose, TVector, TNum>
    {
        var half = Numbers<TNum>.Half;
        List<TPose> result = [];
        foreach (var polyline in segs)
        {
            var curPts = polyline.Poses;
            if (curPts.Length is 0 or 1)
                continue;
            if (result.Count != 0)
            {
                result[^1]=TPose.Lerp(result[^1], curPts[0], half);
                curPts = curPts[1..];
            }
            result.AddRange(curPts);
        }

        return result.ToArray();
    }

    public static TNum GetLength<TNum>(IReadOnlyList<TNum> cumulativeLengths)
    where TNum:INumber<TNum>
    {
        if (cumulativeLengths.Count < 2)
            return TNum.Zero;
        return cumulativeLengths[^1] - cumulativeLengths[0];
    }
}