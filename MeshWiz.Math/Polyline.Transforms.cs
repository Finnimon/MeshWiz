using System.Numerics;

namespace MeshWiz.Math;

public static partial class Polyline
{
    public static class Transforms
    {
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
                var intersectionBefore = Line.TryIntersect(cur, lines[p], out var before);
                var intersectionAfter = Line.TryIntersect(cur, lines[nx], out var after);
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
                _ = Line.TryIntersect(a, b, out var t);
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
                var intersectionBefore = Line.TryIntersect(cur, lines[p], out var before);
                var intersectionAfter = Line.TryIntersect(cur, lines[nx], out var after);
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
                _ = Line.TryIntersect(a, b, out var t);
                points[i + 1] = a.Traverse(t);
            }

            points[0] = points[^1];

            return new Polyline<Vector2<TNum>, TNum>(points);
        }

        // public static Polyline<Vector2<TNum>, TNum>[] ShrinkUntilDegenerated<TNum>(
        //     Polyline<Vector2<TNum>, TNum> toShrink,
        //     TNum stepSize,
        //     bool checkWindingOrder = true)
        //     where TNum : unmanaged, IFloatingPointIeee754<TNum>
        // {
        //     
        // }


        public static Polyline<Vector2<TNum>, TNum> InflateFast<TNum>(Polyline<Vector2<TNum>, TNum> polyline,
            TNum amount)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            if (polyline.Count < 1) return Polyline<Vector2<TNum>, TNum>.Empty;
            var isClosed = polyline.IsClosed;

            var prevLine = polyline[0];
            prevLine += prevLine.NormalDirection.Right * amount;
            var lastLine = polyline[^1];
            lastLine += lastLine.NormalDirection.Right * amount;
            var interSectionPoints = new Vector2<TNum>[polyline.Points.Length];
            if (!isClosed)
            {
                interSectionPoints[0] = prevLine.Start;
                interSectionPoints[^1] = lastLine.End;
            }
            else
            {
                Line.TryIntersect(prevLine, lastLine, out var alongA);
                var p = prevLine.Traverse(alongA);
                interSectionPoints[0] = p;
                interSectionPoints[^1] = p;
            }

            for (var i = 1; i < polyline.Count; i++)
            {
                var line = polyline[i];
                line += line.NormalDirection.Right * amount;
                Line.TryIntersect(prevLine, line, out var intersection);
                var p = prevLine.Traverse(intersection);
                interSectionPoints[i] = p;
            }

            return new Polyline<Vector2<TNum>, TNum>(interSectionPoints);
        }
    }
}