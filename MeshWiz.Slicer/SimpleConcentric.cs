using System.Numerics;
using MeshWiz.Math;
using MeshWiz.Utility;

namespace MeshWiz.Slicer;

public class SimpleConcentric
{
    public static IEnumerable<Polyline<Vector2<TNum>, TNum>> GenPattern<TNum>(Polyline<Vector2<TNum>, TNum> outerBounds,
        TNum pathWidth)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var epsilon = Numbers<TNum>.Eps4;
        if (outerBounds.Count < 3) yield break;
        if (outerBounds.Length < epsilon) yield break;
        if (!outerBounds.IsClosed) yield break;
        outerBounds = outerBounds.CullDeadSegments();
        var halfPathWidth = Numbers<TNum>.Half * pathWidth;
        var wallWinding = Polyline.Evaluate.GetWindingOrder(outerBounds);
        // var isConvex= Polyline.Evaluate.IsConvex(wall);
        if (wallWinding != WindingOrder.CounterClockwise)
            yield break;
        RollingList<(Polyline<Vector2<TNum>, TNum> Polyline, TNum amount, bool isConvex, int depth)> toInflate =
        [
            ..Polyline.Simplicity.MakeSimple(outerBounds, epsilon)
                .Select(polyline=>polyline.CullDeadSegments())
                .Where(polyline => Polyline.Evaluate.GetWindingOrder(polyline) == wallWinding)
                .Where(polyline=>Polyline.Evaluate.SignedArea(polyline)>epsilon)
                .Select(polyline => (polyline, -halfPathWidth, Polyline.Evaluate.IsConvex(polyline), 0))
        ];

        while (toInflate.TryPopFront(out var job))
        {
            var (polyline, amount, isConvex, depth) = job;
            if (!polyline.IsClosed)
            {
                Console.WriteLine($"Not closed at {depth}");
                continue;
            }

            depth++;
            var inflated = Polyline.Transforms.InflateClosedDegenerative(polyline, amount);
            if (inflated.Length < epsilon || inflated.Count < 3) continue;
            if (isConvex && inflated.IsClosed)
            {
                var windingOrder = Polyline.Evaluate.GetWindingOrder(inflated);
                if (windingOrder != WindingOrder.CounterClockwise)
                {
                    Console.WriteLine(windingOrder);
                    continue;
                }
            
                yield return inflated;
                toInflate.PushBack((inflated, -pathWidth, true, depth));
                continue;
            }

            var simplified = Polyline.Simplicity.MakeSimple(inflated, epsilon);
            foreach (var firstCycle in simplified)
            {
                if (!firstCycle.IsClosed)
                {
                    Console.WriteLine($"Not closed at {depth}");
                    Console.WriteLine($"Ends do intersect: {Line.TryIntersect(firstCycle[0],firstCycle[^1],out _)}");
                    continue;
                }
                var culled = firstCycle.CullDeadSegments();
                var signedArea = Polyline.Evaluate.SignedArea(culled);
                if (signedArea < epsilon) continue;
                if (culled.IsClosed != firstCycle.IsClosed)
                {
                    Console.WriteLine($"Culling made non closed at {depth-1}");
                    culled = firstCycle;
                }
                if (Polyline.Simplicity.MultiCheck(culled) != Polyline.Simplicity.Level.Simple)
                {
                    Console.WriteLine(Polyline.Simplicity.MultiCheck(culled));
                    Console.WriteLine(depth - 1);
                    Console.WriteLine("Not Simple");
                    
                    toInflate.PushBack((firstCycle, TNum.Zero,false,depth-1));//retry
                    continue;
                }

                yield return culled;
                toInflate.PushBack((culled, -pathWidth, culled.IsClosed&&Polyline.Evaluate.IsConvex(culled), depth));
            }
        }
    }

    public static SlicedLayer<TNum> GenLayer<TNum>(Plane3<TNum> plane,
        BvhMesh<TNum> mesh, TNum pathWidth)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var intersection = mesh.IntersectRolling(plane);

        List<Polyline<Vector2<TNum>, TNum>> perimeter = [];
        List<Polyline<Vector2<TNum>, TNum>> infill = [];
        var epsilon = TNum.CreateTruncating(0.000001);
        foreach (var outerBounds in intersection)
        {
            if (outerBounds.Count < 3) continue;
            if (outerBounds.Length < epsilon) continue;
            if (!outerBounds.IsClosed) continue;
            var halfPathWidth = Numbers<TNum>.Half * pathWidth;
            var wallWinding = Polyline.Evaluate.GetWindingOrder(outerBounds);
            // var isConvex= Polyline.Evaluate.IsConvex(wall);
            if (wallWinding != WindingOrder.CounterClockwise)
                continue;
            RollingList<(Polyline<Vector2<TNum>, TNum> Polyline, TNum amount, bool isConvex, int depth)> toInflate =
            [
                ..Polyline.Simplicity.MakeSimple(outerBounds, TNum.CreateTruncating(epsilon))
                    .Where(polyline => Polyline.Evaluate.GetWindingOrder(polyline) == wallWinding)
                    .Select(polyline => (polyline, -halfPathWidth, Polyline.Evaluate.IsConvex(polyline), 0))
            ];

            while (toInflate.TryPopFront(out var job))
            {
                var (polyline, amount, isConvex, depth) = job;
                if (!polyline.IsClosed)
                {
                    Console.WriteLine($"Not closed at {depth}");
                }

                var addTo = depth < 1 ? perimeter : infill;
                depth++;
                var inflated = Polyline.Transforms.InflateClosedDegenerative(polyline, amount);
                if (inflated.Length < epsilon || inflated.Count < 3) continue;
                if (isConvex)
                {
                    var windingOrder = Polyline.Evaluate.GetWindingOrder(inflated);
                    if (windingOrder != WindingOrder.CounterClockwise)
                    {
                        Console.WriteLine(windingOrder);
                        continue;
                    }

                    addTo.Add(inflated);
                    toInflate.PushBack((inflated, -pathWidth, true, depth));
                    continue;
                }

                var simplified = Polyline.Simplicity.MakeSimple(inflated, TNum.CreateTruncating(epsilon));
                foreach (var firstCycle in simplified)
                {
                    var signedArea = Polyline.Evaluate.SignedArea(firstCycle);
                    if (signedArea < epsilon) continue;
                    var simple = firstCycle;
                    if (Polyline.Simplicity.MultiCheck(firstCycle) != Polyline.Simplicity.Level.Simple)
                    {
                        Console.WriteLine(Polyline.Simplicity.MultiCheck(firstCycle));
                        Console.WriteLine(depth - 1);
                        Console.WriteLine("Not Simple");

                        // toInflate.PushBack((firstCycle, TNum.Zero,false));
                        // continue;
                    }

                    addTo.Add(simple);
                    toInflate.PushBack((simple, -pathWidth, Polyline.Evaluate.IsConvex(simple), depth));
                }
            }
        }

        var perimeterOptions = TcpOptions.Perimeter | TcpOptions.Additive;
        var perimeterTcp = perimeter.Select(plane.ProjectIntoWorld)
            .Select(perimeterline => perimeterline.Points.ToArray())
            .Select(pts => 
                pts.Select(pt => new ToolCenterPoint<TNum>(pt, plane.Normal, perimeterOptions))
                    .ToArray())
            .ToArray();
        var infillOptions = TcpOptions.Infill | TcpOptions.Additive;
        var infillTcp = perimeter.Select(plane.ProjectIntoWorld)
            .Select(perimeterline => perimeterline.Points.ToArray())
            .Select(pts => 
                pts.Select(pt => new ToolCenterPoint<TNum>(pt, plane.Normal, infillOptions))
                    .ToArray())
            .ToArray();
        return new(perimeterTcp, infillTcp,-plane.D);
    }
}