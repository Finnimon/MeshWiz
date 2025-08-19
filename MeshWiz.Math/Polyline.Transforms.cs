using System.Numerics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public static partial class Polyline
{
    public static class Transforms
    {
        
        private static bool TryIntersectFast<TNum>(
            in Line<Vector2<TNum>, TNum> a,
            in Line<Vector2<TNum>, TNum> b,
            out TNum alongA)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            alongA = default;

            var p = a.Start;
            var r = a.Direction;
            var q = b.Start;
            var s = b.Direction;
            var pq = q - p;
            var rxs = r.Cross(s);

            var colinear = TNum.Abs(rxs) < TNum.CreateTruncating(1e-8);
            if (colinear) return false;

            var t = pq.Cross(s) / rxs;

            alongA = t;
            return true;
        }

        /// <summary>
        /// Shifts all segments by <paramref name="amount"/> towards the outside assuming CCW.
        /// Removes degenerated segments using a quicksort-like range re-check loop until stable.
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="amount">the amount by which the polyline should be inflated</param>
        /// <typeparam name="TNum"></typeparam>
        /// <returns></returns>
        public static Polyline<Vector2<TNum>, TNum> InflateClosedDegenerativeBad<TNum>(
            Polyline<Vector2<TNum>, TNum> polyline,
            TNum amount)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            if (!polyline.IsClosed) throw new ArgumentException("Polyline must be closed", nameof(polyline));

            var n = polyline.Count;
            if (n == 0) return Polyline<Vector2<TNum>, TNum>.Empty;

            var lines = new Line<Vector2<TNum>, TNum>[n];
            for (var i = 0; i < n; i++)
            {
                var seg = polyline[i];
                var segDirection = seg.NormalDirection;
                Vector2<TNum> outsideDirection = new(segDirection.Y, -segDirection.X);
                var offset = outsideDirection * amount;
                lines[i] = new Line<Vector2<TNum>, TNum>(seg.Start + offset, seg.End + offset);
            }

            // Alive flags
            var alive = new bool[n];
            Array.Fill(alive, true);
            var aliveCount = n;

            // Doubly-linked list of indices for O(1) neighbor updates after removals
            var prev = new int[n];
            var next = new int[n];
            for (var i = 0; i < n; i++)
            {
                prev[i] = i - 1;
                next[i] = i + 1;
            }

            prev[0] = n - 1;
            next[^1] = 0;

            var q = new Queue<int>(n);
            var inQueue = new bool[n];
            for (var i = 0; i < n; i++)
            {
                q.Enqueue(i);
                inQueue[i] = true;
            }

            while (q.Count > 0)
            {
                var i = q.Dequeue();
                inQueue[i] = false;

                if (!alive[i]) continue;
                if (aliveCount < 3) return Polyline<Vector2<TNum>, TNum>.Empty;

                var p = prev[i];
                var nx = next[i];

                // If neighbors collapsed to self (only one or two alive), remove
                if (p == i || nx == i)
                {
                    // remove i
                    alive[i] = false;
                    aliveCount--;
                    // update links
                    next[p] = nx;
                    prev[nx] = p;
                    // enqueue neighbors to re-check
                    if (alive[p] && !inQueue[p])
                    {
                        q.Enqueue(p);
                        inQueue[p] = true;
                    }

                    if (alive[nx] && !inQueue[nx])
                    {
                        q.Enqueue(nx);
                        inQueue[nx] = true;
                    }

                    continue;
                }

                var cur = lines[i];
                var intersectionBefore = TryIntersectFast(cur, lines[p], out var before);
                var intersectionAfter = TryIntersectFast(cur, lines[nx], out var after);
                var currentDegenerated = !intersectionBefore || !intersectionAfter || before > after;

                if (currentDegenerated)
                {
                    // remove i
                    alive[i] = false;
                    aliveCount--;
                    // update links
                    next[p] = nx;
                    prev[nx] = p;
                    // enqueue neighbors to re-check
                    if (alive[p] && !inQueue[p])
                    {
                        q.Enqueue(p);
                        inQueue[p] = true;
                    }

                    if (alive[nx] && !inQueue[nx])
                    {
                        q.Enqueue(nx);
                        inQueue[nx] = true;
                    }
                }
            }

            if (aliveCount < 3) return Polyline<Vector2<TNum>, TNum>.Empty;

            // Compact remaining lines into front of array using the linked list
            var firstAlive = -1;
            for (var i = 0; i < n; i++)
                if (alive[i])
                {
                    firstAlive = i;
                    break;
                }

            if (firstAlive == -1) return Polyline<Vector2<TNum>, TNum>.Empty;

            var compact = new Line<Vector2<TNum>, TNum>[aliveCount];
            var idx = 0;
            var curIdx = firstAlive;
            do
            {
                compact[idx++] = lines[curIdx];
                curIdx = next[curIdx];
            } while (curIdx != firstAlive && idx < aliveCount);

            // Compute intersection points between consecutive compact lines
            var m = aliveCount;
            var pCount = m + 1;
            var points = new Vector2<TNum>[pCount];
            for (var i = 0; i < m; i++)
            {
                var a = compact[i];
                var b = compact[(i + 1) % m];
                _ = TryIntersectFast(a, b, out var t);
                points[i + 1] = a.Traverse(t);
            }

            points[0] = points[^1];

            return new Polyline<Vector2<TNum>, TNum>(points);
        }


        /// <summary>
        /// Shifts all segments by <paramref name="amount"/> towards the outside assuming CCW.
        /// Removes degenerated segments using a quicksort-like range re-check loop until stable.
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="amount">the amount by which the polyline should be inflated</param>
        /// <typeparam name="TNum"></typeparam>
        /// <returns></returns>
        public static Polyline<Vector2<TNum>, TNum> InflateClosedDegenerative<TNum>(
            Polyline<Vector2<TNum>, TNum> polyline,
            TNum amount)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            if (!polyline.IsClosed) throw new ArgumentException("Polyline must be closed", nameof(polyline));

            var n = polyline.Count;
            if (n == 0) return Polyline<Vector2<TNum>, TNum>.Empty;

            var lines = new Line<Vector2<TNum>, TNum>[n];
            for (var i = 0; i < n; i++)
            {
                var seg = polyline[i];
                var segDirection = seg.NormalDirection;
                Vector2<TNum> outsideDirection = new(segDirection.Y, -segDirection.X);
                var offset = outsideDirection * amount;
                lines[i] = new Line<Vector2<TNum>, TNum>(seg.Start + offset, seg.End + offset);
            }

            // Alive flags
            var alive = new bool[n];
            Array.Fill(alive, true);
            var aliveCount = n;

            // Doubly-linked list of indices for O(1) neighbor updates after removals
            var prev = new int[n];
            var next = new int[n];
            for (var i = 0; i < n; i++)
            {
                prev[i] = i - 1;
                next[i] = i + 1;
            }

            prev[0] = n - 1;
            next[^1] = 0;

            // Queue of candidates to check and a guard to avoid enqueue duplicates
            var q = new Queue<int>(Enumerable.Range(0, n));
            var inQueue = new bool[n];
            Array.Fill(inQueue, true);

            while (q.Count > 0)
            {
                var i = q.Dequeue();
                inQueue[i] = false;

                if (!alive[i]) continue;
                if (aliveCount < 3) return Polyline<Vector2<TNum>, TNum>.Empty;

                var p = prev[i];
                var nx = next[i];

                // If neighbors collapsed to self (only one or two alive), remove
                if (p == i || nx == i)
                {
                    // remove i
                    alive[i] = false;
                    aliveCount--;
                    // update links
                    next[p] = nx;
                    prev[nx] = p;
                    // enqueue neighbors to re-check
                    if (alive[p] && !inQueue[p])
                    {
                        q.Enqueue(p);
                        inQueue[p] = true;
                    }

                    if (alive[nx] && !inQueue[nx])
                    {
                        q.Enqueue(nx);
                        inQueue[nx] = true;
                    }

                    continue;
                }

                var cur = lines[i];
                var intersectionBefore = TryIntersectFast(cur, lines[p], out var before);
                var intersectionAfter = TryIntersectFast(cur, lines[nx], out var after);
                var currentDegenerated = !intersectionBefore || !intersectionAfter || before > after;

                if (currentDegenerated)
                {
                    // remove i
                    alive[i] = false;
                    aliveCount--;
                    // update links
                    next[p] = nx;
                    prev[nx] = p;
                    // enqueue neighbors to re-check
                    if (alive[p] && !inQueue[p])
                    {
                        q.Enqueue(p);
                        inQueue[p] = true;
                    }

                    if (alive[nx] && !inQueue[nx])
                    {
                        q.Enqueue(nx);
                        inQueue[nx] = true;
                    }
                }
            }

            if (aliveCount < 3) return Polyline<Vector2<TNum>, TNum>.Empty;

            // Compact remaining lines into front of array using the linked list
            var firstAlive = -1;
            for (var i = 0; i < n; i++)
                if (alive[i])
                {
                    firstAlive = i;
                    break;
                }

            if (firstAlive == -1) return Polyline<Vector2<TNum>, TNum>.Empty;

            var compact = new Line<Vector2<TNum>, TNum>[aliveCount];
            var idx = 0;
            var curIdx = firstAlive;
            do
            {
                compact[idx++] = lines[curIdx];
                curIdx = next[curIdx];
            } while (curIdx != firstAlive && idx < aliveCount);

            // Compute intersection points between consecutive compact lines
            var m = aliveCount;
            var pCount = m + 1;
            var points = new Vector2<TNum>[pCount];
            for (var i = 0; i < m; i++)
            {
                var a = compact[i];
                var b = compact[(i + 1) % m];
                _ = TryIntersectFast(a, b, out var t);
                points[i + 1] = a.Traverse(t);
            }

            points[0] = points[^1];

            return new Polyline<Vector2<TNum>, TNum>(points);
        }
    }
}