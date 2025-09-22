using System.Numerics;
using MeshWiz.Utility;

namespace MeshWiz.Math;

public static partial class Polyline
{
    public static class Reduction
    {
        public static Polyline<TVec, TNum> DouglasPeucker<TVec, TNum>(Polyline<TVec, TNum> polyline, TNum epsilon)
            where TVec : unmanaged, IFloatingVector<TVec, TNum>
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            epsilon = TNum.Abs(epsilon);
            if (epsilon < TNum.Epsilon) return polyline;

            var pts = polyline.Points;
            var ptSpan = pts.AsSpan();
            if (pts.Length < 3) return polyline;
            var keep = new bool[pts.Length];
            keep[0] = true;
            keep[^1] = true;
            var keepCount = 2;
            RollingList<Range> jobs = [Range.All];
            epsilon*=epsilon; //squared for faster comparisons
            while (jobs.TryPopFront(out var range))
            {
                var (start, length) = range.GetOffsetAndLength(pts.Length);
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
    }
}