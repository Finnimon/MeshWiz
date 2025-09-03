using System.Numerics;
using MeshWiz.Math;

namespace MeshWiz.Slicer;

public sealed class ConcentricHierarchy<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public static ConcentricHierarchy<TNum> Create(IReadOnlyList<Polyline<Vector2<TNum>, TNum>> polygons)
    {

        foreach (var polyline in polygons)
            if (!polyline.IsClosed) polyline.Points[^1] = polyline.Points[0]; //makeClosed

        var withArea = polygons.Select(pl =>
                (pl, signedArea: Polyline.Evaluate.SignedArea(pl)))
            .OrderBy(x => x.signedArea)
            .ToArray();

        List<Polyline<Vector2<TNum>, TNum>> booleanAddCollector = [];
        List<Polyline<Vector2<TNum>, TNum>> booleanRemoveCollector = [];
        
        for (var i = 0; i < withArea.Length; i++)
        {
            if(withArea[i].signedArea<TNum.Zero) booleanRemoveCollector.Add(withArea[i].pl);
            if(withArea[i].signedArea>TNum.Zero) booleanAddCollector.Add(withArea[i].pl);
        }
        booleanRemoveCollector.Reverse();

        var booleanAdd = booleanAddCollector.OrderBy(pl => Polyline.Evaluate.Area(pl)).ToArray();
        var booleanRemove = booleanRemoveCollector.ToArray();

        // booleanAdd = Polyline.Boolean.Combine(booleanAdd);
        // booleanRemove = Polyline.Boolean.Combine(booleanRemove);


        throw new NotImplementedException();
    }
}