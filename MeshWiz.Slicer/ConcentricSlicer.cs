using System.Numerics;
using MeshWiz.Math;

namespace MeshWiz.Slicer;

public static class ConcentricSlicer
{
    public static (Polyline<Vector2<TNum>, TNum>[] perimeter, Polyline<Vector2<TNum>, TNum>[] infill)
        GeneratePath<TNum>(Polyline<Vector2<TNum>, TNum>[] bounds)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        => throw new NotImplementedException();
    //
    // private class ConcentricHierarchy<TNum>
    //     where TNum : unmanaged, IFloatingPointIeee754<TNum>
    // {
    //     public List<Polyline<Vector2<TNum>, TNum>>? Ccw;
    //     public List<Polyline<Vector2<TNum>, TNum>>? Cw;
    //     public int[]? Roots;
    //
    //     public static ConcentricHierarchy<TNum> Create(Polyline<Vector2<TNum>, TNum>[] polylines)
    //     {
    //         List<Polyline<Vector2<TNum>, TNum>> ccw = [];
    //         List<Polyline<Vector2<TNum>, TNum>> cw = [];
    //         foreach (var polyline in polylines)
    //         {
    //             var wo = Polyline.Evaluate.GetWindingOrder(polyline);
    //             if(wo==WindingOrder.CounterClockwise)ccw.Add(polyline);
    //             else if(wo==WindingOrder.Clockwise)cw.Add(polyline);
    //         }
    //         
    //      throw new NotImplementedException();
    //         
    //     }
    }
