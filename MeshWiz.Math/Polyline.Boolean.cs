// File: Polyline.Boolean and BBox2<TNum>.cs
// Adds a generic BBox2<TNum> and moves self-contained boolean/hierarchy logic
// into Polyline.Boolean nested static class. No external deps required.

namespace MeshWiz.Math;


public static partial class Polyline
{
    public static partial class Boolean
    {
        // // Public Hierarchy node type returned by the processing pipeline
        // public sealed class HierarchyNode<TNum>
        //     where TNum : unmanaged, IFloatingPointIeee754<TNum>
        // {
        //     public Polyline<Vec2<TNum>, TNum> Polygon { get; }
        //     public WindingOrder Winding { get; }
        //     public HierarchyNode<TNum>? Parent { get; internal set; }
        //     public List<HierarchyNode<TNum>> Children { get; } = new();
        //     internal bool Removed { get; set; } = false;
        //
        //     public HierarchyNode(Polyline<Vec2<TNum>, TNum> p)
        //     {
        //         Polygon = p;
        //         Winding = Evaluate.GetWindingOrder(p);
        //     }
        // }
        //
        // // Entry point: combine inputs and produce hierarchy roots after applying filters
        // public static IReadOnlyList<HierarchyNode<TNum>> BuildIslandsHierarchyAndFilter<TNum>(IEnumerable<Polyline<Vec2<TNum>, TNum>> inputs)
        //     where TNum : unmanaged, IFloatingPointIeee754<TNum>
        // {
        //     // 1) Boolean combine according to winding
        //     var combined = CombinePolylines(inputs);
        //
        //     // 2) Build nodes and containment hierarchy
        //     var nodes = combined.Select(p => new HierarchyNode<TNum>(p)).ToList();
        //     for (var i = 0; i < nodes.Count; i++)
        //     {
        //         var a = nodes[i];
        //         var ca = a.Polygon.VertexCentroid;
        //         HierarchyNode<TNum>? bestParent = null;
        //         var bestArea = TNum.PositiveInfinity;
        //         for (var j = 0; j < nodes.Count; j++)
        //         {
        //             if (i == j) continue;
        //             var b = nodes[j];
        //             if (!PointInPolygonGeneric(b.Polygon, ca)) continue;
        //             var area = Evaluate.Area(b.Polygon);
        //             if (area >= bestArea) continue;
        //             bestArea = area;
        //             bestParent = b;
        //         }
        //
        //         if (bestParent == null) continue;
        //         a.Parent = bestParent;
        //         bestParent.Children.Add(a);
        //     }
        //
        //     // 3) Apply rules
        //     // Promote CCW inside CW inside CCW to root
        //     foreach (var node in nodes)
        //     {
        //         if (node.Winding == WindingOrder.CounterClockwise
        //             && node.Parent != null
        //             && node.Parent.Winding == WindingOrder.Clockwise
        //             && node.Parent.Parent != null
        //             && node.Parent.Parent.Winding == WindingOrder.CounterClockwise)
        //         {
        //             node.Parent.Children.Remove(node);
        //             node.Parent = null;
        //         }
        //     }
        //
        //     // Remove CCW fully contained in CCW parent
        //     foreach (var node in nodes.Where(n => n.Winding == WindingOrder.CounterClockwise && n.Parent != null && n.Parent.Winding == WindingOrder.CounterClockwise).ToArray())
        //     {
        //         var parent = node.Parent!;
        //         foreach (var ch in node.Children.ToArray())
        //         {
        //             ch.Parent = parent;
        //             parent.Children.Add(ch);
        //         }
        //         node.Children.Clear();
        //         parent.Children.Remove(node);
        //         node.Parent = null;
        //         node.Removed = true;
        //     }
        //
        //     // Remove CW not contained or intersecting any CCW
        //     for (var i = 0; i < nodes.Count; i++)
        //     {
        //         var n = nodes[i];
        //         if (n.Winding != WindingOrder.Clockwise) continue;
        //         var hasCCWancestor = false;
        //         var p = n.Parent;
        //         while (p != null)
        //         {
        //             if (p.Winding == WindingOrder.CounterClockwise) { hasCCWancestor = true; break; }
        //             p = p.Parent;
        //         }
        //         if (hasCCWancestor) continue;
        //
        //         var nb = BBox2<TNum>.FromPolyline(n.Polygon);
        //         var intersectsSomeCCW = false;
        //         foreach (var ccw in nodes.Where(x => x.Winding == WindingOrder.CounterClockwise))
        //         {
        //             var cb = BBox2<TNum>.FromPolyline(ccw.Polygon);
        //             if (nb.Overlaps(cb)) { intersectsSomeCCW = true; break; }
        //         }
        //         if (!intersectsSomeCCW)
        //         {
        //             var parent = n.Parent;
        //             foreach (var ch in n.Children.ToArray())
        //             {
        //                 ch.Parent = parent;
        //                 if (parent != null) parent.Children.Add(ch);
        //             }
        //             if (parent != null) parent.Children.Remove(n);
        //             n.Children.Clear();
        //             n.Parent = null;
        //             n.Removed = true;
        //         }
        //     }
        //
        //     var roots = nodes.Where(n => n.Parent == null && !n.Removed).ToList();
        //     return roots;
        // }
        //
        // // ---------------------- Core polygon boolean helpers (generic) ----------------------
        // // We'll implement a Greiner-Hormann inspired boolean routine but keep it generic by
        // // converting TNum -> TNum for geometric ops, and returning Polyline<Vec2<TNum>,TNum>.
        //
        // // simple vector conversion helpers
        // private static Vec2<TNum> ToD<TNum>(Vec2<TNum> v) where TNum : unmanaged, IFloatingPointIeee754<TNum>
        //     => new Vec2<TNum>(Convert.ToTNum(v.X), Convert.ToTNum(v.Y));
        //
        // private static Vec2<TNum> FromD<TNum>(Vec2<TNum> v) where TNum : unmanaged, IFloatingPointIeee754<TNum>
        //     => new Vec2<TNum>(TNum.CreateChecked(v.X), TNum.CreateChecked(v.Y));
        //
        // // GH node
        // private sealed class GHNode
        // {
        //     public Vec2<TNum> P;
        //     public bool Intersect;
        //     public TNum Alpha;
        //     public GHNode Other;
        //     public bool Entry;
        //     public bool Visited;
        //     public GHNode Prev;
        //     public GHNode Next;
        //
        //     public GHNode(Vec2<TNum> p) { P = p; Intersect = false; Alpha = 0; Entry = false; Visited = false; }
        // }
        //
        // private static List<GHNode> BuildNodeList<TNum>(Polyline<Vec2<TNum>, TNum> poly)
        //     where TNum : unmanaged, IFloatingPointIeee754<TNum>
        // {
        //     var nodes = new List<GHNode>();
        //     var n = poly.Points.Length - 1;
        //     for (var i = 0; i < n; i++) nodes.Add(new GHNode(ToD(poly.Points[i])));
        //     if (nodes.Count == 0) return nodes;
        //     for (var i = 0; i < nodes.Count; i++)
        //     {
        //         nodes[i].Next = nodes[(i + 1) % nodes.Count];
        //         nodes[i].Prev = nodes[(i - 1 + nodes.Count) % nodes.Count];
        //     }
        //     return nodes;
        // }
        //
        // private static bool SegmentIntersection<TNum>(Vec2<TNum> A, Vec2<TNum> B, Vec2<TNum> C, Vec2<TNum> D,
        //     out Vec2<TNum> intersection, out TNum t, out TNum u)
        // where TNum:unmanaged, IFloatingPointIeee754<TNum>
        // {
        //     intersection = new Vec2<TNum>(); t = u = TNum.NaN;
        //     var r = B - A; var s = D - C;
        //     var rxs = r.Cross(s);
        //     var qpxr = - r.Cross(C-A);
        //     if (rxs.IsApprox(TNum.Zero) && qpxr.IsApprox(TNum.Zero))
        //     {
        //         var rdotr = r.SquaredLength;
        //         if (rdotr.IsApprox(TNum.Zero)) return false;
        //         var t0 = r.Dot(C-A) / rdotr;
        //         var t1 = r.Dot(D-A) / rdotr;
        //         if (t0 > t1) (t0, t1) = (t1, t0);
        //         var lo = TNum.Max(t0, TNum.Zero); var hi = TNum.Min(t1, TNum.One);
        //         if (lo > hi) return false;
        //         t = (lo + hi) * TNum.CreateTruncating(0.5);
        //         intersection = A + t * (B - A);
        //         u = TNum.NaN;
        //         return true;
        //     }
        //     if (rxs.IsApprox(TNum.Zero) && qpxr.IsApprox(TNum.Zero)) return false;
        //     t = -s.Cross(C - A) / rxs;
        //     u = -s.Cross(C - A) / rxs;
        //     if ( t.InsideRange(TNum.Zero,TNum.One) && u.InsideRange(TNum))
        //     {
        //         intersection = A + (float)t * (B - A);
        //         return true;
        //     }
        //     return false;
        // }
        //
        // private static void InsertNodeOnEdge(GHNode start, GHNode toInsert)
        // {
        //     var next = start.Next;
        //     start.Next = toInsert; toInsert.Prev = start; toInsert.Next = next; next.Prev = toInsert;
        //     // collect intersection nodes between start..next and sort by Alpha
        //     var list = new List<GHNode>();
        //     var walker = start.Next;
        //     while (walker != next.Next && walker != start)
        //     {
        //         if (walker.Intersect) list.Add(walker);
        //         walker = walker.Next;
        //         if (walker == null) break;
        //     }
        //     list.Sort((a, b) => a.Alpha.CompareTo(b.Alpha));
        //     var cur = start;
        //     foreach (var n in list)
        //     {
        //         cur.Next = n; n.Prev = cur; cur = n;
        //     }
        //     cur.Next = next; next.Prev = cur;
        // }
        //
        // private static void FindAndInsertIntersections(List<GHNode> subj, List<GHNode> clip)
        // {
        //     var sn = subj.Count; var cn = clip.Count;
        //     if (sn == 0 || cn == 0) return;
        //     for (var i = 0; i < sn; i++)
        //     {
        //         var a1 = subj[i]; var a2 = a1.Next; var A = a1.P; var B = a2.P;
        //         for (var j = 0; j < cn; j++)
        //         {
        //             var b1 = clip[j]; var b2 = b1.Next; var C = b1.P; var D = b2.P;
        //             if (SegmentIntersection(A, B, C, D, out var ip, out var t, out var u))
        //             {
        //                 var sNode = new GHNode(ip) { Intersect = true, Alpha = t };
        //                 var cNode = new GHNode(ip) { Intersect = true, Alpha = u };
        //                 sNode.Other = cNode; cNode.Other = sNode;
        //                 InsertNodeOnEdge(a1, sNode);
        //                 InsertNodeOnEdge(b1, cNode);
        //             }
        //         }
        //     }
        // }
        //
        // private static bool PointInPolygon(List<Vec2<TNum>> pts, Vec2<TNum> p)
        // {
        //     if (pts == null || pts.Count == 0) return false;
        //     var inside = false; for (int i = 0, j = pts.Count - 1; i < pts.Count; j = i++)
        //     {
        //         var xi = pts[i].X; var yi = pts[i].Y; var xj = pts[j].X; var yj = pts[j].Y;
        //         var intersect = ((yi > p.Y) != (yj > p.Y)) && (p.X < (xj - xi) * (p.Y - yi) / (yj - yi + 1e-300) + xi);
        //         if (intersect) inside = !inside;
        //     }
        //     return inside;
        // }
        //
        // private static void MarkEntryExit(List<GHNode> subj, List<GHNode> clip, BoolOp op)
        // {
        //     // For each intersection node in subj, determine if a small step forward lies inside clip
        //     var clipPts = clip.Select(n => n.P).ToList();
        //     foreach (var s in subj)
        //     {
        //         if (!s.Intersect) continue;
        //         var after = s.Next != null ? (s.P + s.Next.P) * 0.5f : s.P;
        //         var inside = PointInPolygon(clipPts, after);
        //         s.Entry = inside;
        //         if (s.Other != null) s.Other.Entry = inside;
        //     }
        // }
        //
        // private static List<List<Vec2<TNum>>> TraverseAndExtract(List<GHNode> subj, List<GHNode> clip, BoolOp op)
        // {
        //     var results = new List<List<Vec2<TNum>>>();
        //     foreach (var n in subj) if (n.Intersect) n.Visited = false;
        //     foreach (var n in clip) if (n.Intersect) n.Visited = false;
        //
        //     var hasInter = subj.Any(n => n.Intersect) || clip.Any(n => n.Intersect);
        //     if (!hasInter)
        //     {
        //         var subjPts = subj.Select(n => n.P).ToList();
        //         var clipPts = clip.Select(n => n.P).ToList();
        //         var subjInClip = subjPts.Count>0 && PointInPolygon(clipPts, subjPts[0]);
        //         var clipInSubj = clipPts.Count>0 && PointInPolygon(subjPts, clipPts[0]);
        //         if (op == BoolOp.Union)
        //         {
        //             if (subjInClip) results.Add(clipPts);
        //             else if (clipInSubj) results.Add(subjPts);
        //             else { results.Add(subjPts); results.Add(clipPts); }
        //         }
        //         else if (op == BoolOp.Difference)
        //         {
        //             if (subjInClip) { }
        //             else if (clipInSubj) { results.Add(subjPts); results.Add(clipPts); }
        //             else results.Add(subjPts);
        //         }
        //         return results;
        //     }
        //
        //     foreach (var sStart in subj.Where(n => n.Intersect && !n.Visited))
        //     {
        //         var s = sStart;
        //         var poly = new List<Vec2<TNum>>();
        //         while (true)
        //         {
        //             if (s.Visited && s == sStart) break;
        //             if (s.Intersect)
        //             {
        //                 if (s.Visited) break;
        //                 s.Visited = true;
        //                 poly.Add(s.P);
        //                 var followClip = s.Entry;
        //                 var o = s.Other;
        //                 if (o == null) { s = s.Next; continue; }
        //                 o.Visited = true;
        //                 s = followClip ? o.Next : s.Next;
        //             }
        //             else
        //             {
        //                 poly.Add(s.P);
        //                 s = s.Next;
        //             }
        //             if (poly.Count > 0 && Vec2.DistanceSquared(poly[0], sStart.P) < 1e-18 && s.Intersect) break;
        //             if (poly.Count > 10000) break;
        //         }
        //         if (poly.Count >= 3) results.Add(poly);
        //     }
        //
        //     if (results.Count == 0)
        //     {
        //         var subjPts = subj.Select(n => n.P).ToList();
        //         if (op == BoolOp.Union) results.Add(subjPts);
        //         else if (op == BoolOp.Difference)
        //         {
        //             var final = new List<Vec2<TNum>>();
        //             var n = subjPts.Count;
        //             for (var i = 0; i < n; i++)
        //             {
        //                 var a = subjPts[i]; var b = subjPts[(i + 1) % n]; var mid = (a + b) * 0.5f;
        //                 if (!PointInPolygon(clip.Select(p => p.P).ToList(), mid))
        //                 {
        //                     if (final.Count == 0 || final[^1] != a) final.Add(a);
        //                 }
        //             }
        //             if (final.Count >= 3) results.Add(final);
        //         }
        //     }
        //
        //     return results;
        // }
        //
        // private enum BoolOp { Union, Difference }
        //
        // private static List<Polyline<Vec2<TNum>, TNum>> PolygonBoolean<TNum>(Polyline<Vec2<TNum>, TNum> subjPoly,
        //     Polyline<Vec2<TNum>, TNum> clipPoly, BoolOp op)
        //     where TNum : unmanaged, IFloatingPointIeee754<TNum>
        // {
        //     var subjNodes = BuildNodeList(subjPoly);
        //     var clipNodes = BuildNodeList(clipPoly);
        //     if (subjNodes.Count == 0) return new();
        //     if (clipNodes.Count == 0) return new() { subjPoly };
        //     FindAndInsertIntersections(subjNodes, clipNodes);
        //     MarkEntryExit(subjNodes, clipNodes, op);
        //     var outList = TraverseAndExtract(subjNodes, clipNodes, op);
        //     var result = new List<Polyline<Vec2<TNum>, TNum>>();
        //     foreach (var path in outList)
        //     {
        //         if (path == null || path.Count < 3) continue;
        //         var pts = new Vec2<TNum>[path.Count + 1];
        //         for (var i = 0; i < path.Count; i++) pts[i] = FromD<TNum>(path[i]);
        //         pts[^1] = pts[0];
        //         result.Add(new Polyline<Vec2<TNum>, TNum>(pts));
        //     }
        //     return result;
        // }
        //
        // public static List<Polyline<Vec2<TNum>, TNum>> CombinePolylines<TNum>(IEnumerable<Polyline<Vec2<TNum>, TNum>> inputs)
        //     where TNum : unmanaged, IFloatingPointIeee754<TNum>
        // {
        //     var result = new List<Polyline<Vec2<TNum>, TNum>>();
        //     var polys = inputs.Where(p => p.IsClosed).ToList();
        //     if (polys.Count == 0) return result;
        //     foreach (var poly in polys)
        //     {
        //         var winding = Evaluate.GetWindingOrder(poly);
        //         if (winding == WindingOrder.CounterClockwise)
        //         {
        //             if (result.Count == 0) { result.Add(poly); continue; }
        //             var nextResult = new List<Polyline<Vec2<TNum>, TNum>>();
        //             foreach (var r in result)
        //             {
        //                 var rb = BBox2<TNum>.FromPolyline(r);
        //                 var pb = BBox2<TNum>.FromPolyline(poly);
        //                 if (!rb.Overlaps(pb)) nextResult.Add(r);
        //                 else
        //                 {
        //                     var merged = PolygonBoolean(r, poly, BoolOp.Union);
        //                     if (merged.Count > 0) nextResult.AddRange(merged);
        //                 }
        //             }
        //             var mergedIntoExisting = nextResult.Any(r => Evaluate.SignedArea<TNum>(r).EpsilonTruncatingSign() != 0 && PointInPolylineCentroid(poly, r));
        //             if (!mergedIntoExisting) nextResult.Add(poly);
        //             result = CollapsePolylines(nextResult);
        //         }
        //         else if (winding == WindingOrder.Clockwise)
        //         {
        //             if (result.Count == 0) continue;
        //             var nextResult = new List<Polyline<Vec2<TNum>, TNum>>();
        //             foreach (var r in result)
        //             {
        //                 var rb = BBox2<TNum>.FromPolyline(r);
        //                 var pb = BBox2<TNum>.FromPolyline(poly);
        //                 if (!rb.Overlaps(pb)) nextResult.Add(r);
        //                 else
        //                 {
        //                     var diff = PolygonBoolean(r, poly, BoolOp.Difference);
        //                     if (diff.Count > 0) nextResult.AddRange(diff);
        //                 }
        //             }
        //             result = CollapsePolylines(nextResult);
        //         }
        //     }
        //     for (var i = 0; i < result.Count; i++)
        //     {
        //         var p = result[i];
        //         if (!p.IsClosed)
        //         {
        //             var pts = new Vec2<TNum>[p.Points.Length + 1]; Array.Copy(p.Points, pts, p.Points.Length); pts[^1] = pts[0]; result[i] = new Polyline<Vec2<TNum>, TNum>(pts);
        //         }
        //     }
        //     return result;
        // }
        //
        // private static bool PointInPolylineCentroid<TNum>(Polyline<Vec2<TNum>, TNum> a, Polyline<Vec2<TNum>, TNum> b)
        //     where TNum : unmanaged, IFloatingPointIeee754<TNum>
        // {
        //     var c = a.VertexCentroid; return PointInPolygonGeneric(b, c);
        // }
        //
        // private static bool PointInPolygonGeneric<TNum>(Polyline<Vec2<TNum>, TNum> poly, Vec2<TNum> pt)
        //     where TNum : unmanaged, IFloatingPointIeee754<TNum>
        // {
        //     if (!poly.IsClosed) return false;
        //     var inside = false; var n = poly.Points.Length - 1;
        //     var px = Convert.ToTNum(pt.X); var py = Convert.ToTNum(pt.Y);
        //     for (int i = 0, j = n - 1; i < n; j = i++)
        //     {
        //         var xi = Convert.ToTNum(poly.Points[i].X); var yi = Convert.ToTNum(poly.Points[i].Y);
        //         var xj = Convert.ToTNum(poly.Points[j].X); var yj = Convert.ToTNum(poly.Points[j].Y);
        //         var intersect = ((yi > py) != (yj > py)) && (px < (xj - xi) * (py - yi) / (yj - yi + 1e-300) + xi);
        //         if (intersect) inside = !inside;
        //     }
        //     return inside;
        // }
        //
        // private static List<Polyline<Vec2<TNum>, TNum>> CollapsePolylines<TNum>(List<Polyline<Vec2<TNum>, TNum>> polys)
        //     where TNum : unmanaged, IFloatingPointIeee754<TNum>
        // {
        //     var outList = new List<Polyline<Vec2<TNum>, TNum>>();
        //     foreach (var p in polys)
        //     {
        //         if (p.Points.Length < 4) continue;
        //         var found = outList.Any(q =>
        //         {
        //             var da = Math.Abs(Convert.ToTNum(Evaluate.SignedArea(p)) - Convert.ToTNum(Evaluate.SignedArea(q)));
        //             var dc = Vec2.DistanceSquared(new Vec2<float>(Convert.ToSingle(p.VertexCentroid.X), Convert.ToSingle(p.VertexCentroid.Y)),
        //                                              new Vec2<float>(Convert.ToSingle(q.VertexCentroid.X), Convert.ToSingle(q.VertexCentroid.Y)));
        //             return da < 1e-6 && dc < 1e-8;
        //         });
        //         if (!found) outList.Add(p);
        //     }
        //     return outList;
        // }
    }
}
