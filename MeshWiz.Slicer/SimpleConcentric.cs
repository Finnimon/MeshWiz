using System.Numerics;
using MeshWiz.Math;

namespace MeshWiz.Slicer;

public class SimpleConcentric
{
    public static IEnumerable<Polyline<Vector2<TNum>, TNum>> GenPattern<TNum>(Polyline<Vector2<TNum>, TNum> wall, TNum pathWidth)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        if(!wall.IsClosed) wall.Points[^1]=wall.Points[0];//make closed
        var halfPathWidth=Numbers<TNum>.Half*pathWidth;
        var wallWinding = Polyline.Evaluate.GetWindingOrder(wall);
        for(TNum i=TNum.Zero;true;i++)
        {
            var pl=Polyline.Transforms.InflateClosedDegenerative(wall,-pathWidth*i-halfPathWidth);
            if(pl.Length<TNum.CreateTruncating(0.01)) yield break;
            if(Polyline.Evaluate.GetWindingOrder(pl)!=wallWinding) yield break;
            yield return pl;
        }
    }
}