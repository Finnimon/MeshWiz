using System.Numerics;
using MeshWiz.Math;

namespace MeshWiz.Slicer;

public sealed class PlanarSlicer<TNum>(Vector3<TNum> layerNormal, Vector3<TNum> meanderNormal) : ISlicer<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Vector3<TNum> LayerNormal = layerNormal.Normalized, MeanderNormal = meanderNormal.Normalized;

    public IReadOnlyList<ICurve<Vector3<TNum>, TNum>> Slice(IMesh<TNum> mesh, SlicingDirective<TNum> directive)
    {
        var bbox = mesh.BBox;
        var floorPlane = new Plane3<TNum>(layerNormal, bbox.Min);
        var distance = bbox.Size.Length;
        var indexedMesh = mesh.Indexed();
        var shrunkenForOuterPasses = BvhMesh<TNum>.SurfaceAreaHeuristic(Slicing.GrowMesh(indexedMesh, -directive.PathWidth));
        List<Plane3<TNum>> layerList = new(int.CreateTruncating(distance / directive.LayerHeight));
        for (var h = TNum.CreateTruncating(0.000001); h <= distance; h += directive.LayerHeight)
            layerList.Add(new(layerNormal, floorPlane.D - h));
        var layers = layerList.ToArray();
        var outerPassesTask = Task.Run(() => CreateOuterPasses(shrunkenForOuterPasses, layers));
        var shrunkenForInfill = BvhMesh<TNum>.SurfaceAreaHeuristic(Slicing.GrowMesh(shrunkenForOuterPasses, -directive.PathWidth));
        var infill=CreateInfill(shrunkenForInfill, layers,directive.PathWidth);
        return [];
    }

    private IEnumerable<Polyline<Vector3<TNum>, TNum>[]> CreateInfill(BvhMesh<TNum> shrunkenForInfill, Plane3<TNum>[] layers, TNum directivePathWidth) 
        => layers.Select(plane3 => CreateInfillLayer(shrunkenForInfill, plane3, directivePathWidth))
            .Where(layerInfill => layerInfill is {Length:>0});

    private Polyline<Vector3<TNum>, TNum>[] CreateInfillLayer(BvhMesh<TNum> offsetMesh, Plane3<TNum> layer, TNum pathWidth)=> throw new NotImplementedException();

    private IReadOnlyList<Polyline<Vector3<TNum>, TNum>[]> CreateOuterPasses(BvhMesh<TNum> offsetMesh,
        Plane3<TNum>[] layers)
    {
        throw new NotImplementedException();
        // return layers.Select(layer => 
        //         offsetMesh.IntersectRolling(layer)
        //         .Where(pl => pl.Length >= TNum.CreateTruncating(0.001))
        //         .ToArray())
        //     .ToArray();
    }

    private IReadOnlyList<Polyline<Vector3<TNum>, TNum>[]> CreateInfill(BvhMesh<TNum> offsetMesh,
        Vector3<TNum> layerNormal, TNum[] layerPositions, TNum pathWidth) => throw new NotImplementedException();

}