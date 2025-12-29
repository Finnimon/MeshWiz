using System.Numerics;
using MeshWiz.Collections;
using MeshWiz.Utility;

namespace MeshWiz.Math;

public static partial class Polyline
{
    public static class Reduction
    {
        public static Polyline<TVec, TNum> DouglasPeucker<TVec, TNum>(Polyline<TVec, TNum> polyline, TNum? eps=null)
            where TVec : unmanaged, IVec<TVec, TNum>
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            eps ??= Numbers<TNum>.ZeroEpsilon;
            var epsilon = TNum.Abs(eps.Value);
            if (epsilon < TNum.Epsilon) return polyline;

            var ptSpan = polyline.Points;
            if (ptSpan.Length < 3) return polyline;
            var keep = new bool[ptSpan.Length];
            keep[0] = true;
            keep[^1] = true;
            var keepCount = 2;
            RollingList<Range> jobs = [Range.All];
            epsilon*=epsilon; //squared for faster comparisons
            while (jobs.TryPopFront(out var range))
            {
                var (start, length) = range.GetOffsetAndLength(ptSpan.Length);
                var end = start + length - 1;
                var l = ptSpan[start].LineTo(ptSpan[end]);
                var max = TNum.NegativeInfinity;
                var maxPos = -1;
                for (var i = start + 1; i < end; i++)
                {
                    var p = ptSpan[i];
                    var d = l.SquaredDistanceTo(p);
                    if (d < max) continue;
                    max = d;
                    maxPos = i;
                }
                if(max<epsilon||maxPos==-1) continue;
                keep[maxPos] = true;
                keepCount++;
                if (maxPos - start > 1) jobs.PushFront(start..(maxPos+1));
                if (end - maxPos > 1) jobs.PushFront(maxPos..(end+1));
            }

            var result = new TVec[keepCount];
            keepCount = -1;
            for (var i = 0; i < keep.Length; i++)
                if (keep[i]) result[++keepCount] = ptSpan[i];

            return new Polyline<TVec, TNum>(result);
        }
        public static TPos[] DouglasPeucker<TPos,TVector, TNum>(ReadOnlySpan<TPos> ptSpan, TNum? eps=null)
            where TPos : unmanaged, IPosition<TPos,TVector, TNum>
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
            where TVector : unmanaged, IVec<TVector, TNum>
        {
            eps ??= Numbers<TNum>.ZeroEpsilon;
            var epsilon = TNum.Abs(eps.Value);
            if (epsilon < TNum.Epsilon) return ptSpan.ToArray();

            if (ptSpan.Length < 3) return ptSpan.ToArray();
            var keep = new bool[ptSpan.Length];
            keep[0] = true;
            keep[^1] = true;
            var keepCount = 2;
            RollingList<Range> jobs = [Range.All];
            epsilon*=epsilon; //squared for faster comparisons
            while (jobs.TryPopFront(out var range))
            {
                var (start, length) = range.GetOffsetAndLength(ptSpan.Length);
                var end = start + length - 1;
                var l = ptSpan[start].Position.LineTo(ptSpan[end].Position);
                var max = TNum.NegativeInfinity;
                var maxPos = -1;
                for (var i = start + 1; i < end; i++)
                {
                    var p = ptSpan[i];
                    var d = l.SquaredDistanceTo(p.Position);
                    if (d < max) continue;
                    max = d;
                    maxPos = i;
                }
                if(max<epsilon||maxPos==-1) continue;
                keep[maxPos] = true;
                keepCount++;
                if (maxPos - start > 1) jobs.PushFront(start..(maxPos+1));
                if (end - maxPos > 1) jobs.PushFront(maxPos..(end+1));
            }

            var result = new TPos[keepCount];
            keepCount = -1;
            for (var i = 0; i < keep.Length; i++)
                if (keep[i]) result[++keepCount] = ptSpan[i];

            return result;
        }

    }
}