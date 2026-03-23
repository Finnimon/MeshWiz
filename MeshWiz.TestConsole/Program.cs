using System.Diagnostics;
using MathNet.Numerics.LinearAlgebra.Solvers;
using MeshWiz.Collections;
using MeshWiz.Math;
using MeshWiz.RefLinq;
using MeshWiz.Utility;

Console.WriteLine("Hello World!");
DoStuff();

// Console.WriteLine($"Total intersection time {sw.Elapsed} for lines {buf.Count}");

    static (IndexedMesh<double> halfCyl, List<Polyline<Vec3<double>, double>> layers) DoStuff()
    {
        var halfCyl = CreateHalfCyl().Indexed();
        Console.WriteLine(halfCyl.Count);
        var sw = Stopwatch.StartNew();
        var bvh = Bvh.Mesh<double>.Sah(halfCyl);
        Console.WriteLine($"Create bvh {sw.Elapsed}");
        sw.Restart();

        var normal = Vec3<double>.Create(1, 0, 1).Normalized();
        var plane = new Plane<double>(normal, 0);
        var start = plane.SignedDistance(bvh.BBox.Min);
        var end = plane.SignedDistance(bvh.BBox.Max);
        var allLayers = new List<Polyline<Vec3<double>, double>>();
        const int layerCount = 10000;
        var buf=new RollingList<Line<Vec2<double>, double>>();
        for(var i=0;i<= layerCount;i++)
        {
            var pt = Vec3<double>.Lerp(bvh.BBox.Min, bvh.BBox.Max, ((double)i) / (double)layerCount);
            var lplane = new Plane<double>(plane.Normal, pt);
            buf.Clear();
            var pls = bvh.Intersect(lplane,buf);
            // var polys=Polyline.Creation.UnifyNonReversing(pls).Iterate().Select(lplane.ProjectIntoWorld);
            // blLayer.AddRange(polys.ToArray());
        }
        Console.WriteLine($"intersection time, {sw.Elapsed}");
        return (halfCyl, allLayers);
    }
    static Mesh<double> CreateHalfCyl()
    {
        var arc = new Arc3<double>(new Circle3<double>(default, Vec3<double>.UnitX, 1.0), -0.5*double.Pi,0.5*double.Pi);
        var outer = arc.ToPolyline(new PolylineTessellationParameter<double>{MaxAngularDeviation=0.001});
        var inner = outer.TransformedBy(Mat3x3<double>.CreateScalar(0.9)).Reversed();
        Vec3<double>[] pts = [..outer.Points, ..inner.Points, outer.Points[0]];
        Polyline<Vec3<double>, double> pl = new(pts);
        var innerLine = outer.TransformedBy(Mat3x3<double>.CreateScalar(0.9));
        var posTrans = Transforms<double>.Translation(Vec3<double>.UnitX);
        var negTrans = Transforms<double>.Translation(-Vec3<double>.UnitX);
        var posOffset = pl.TransformedBy(posTrans);
        var negOffset = pl.TransformedBy(negTrans);
        var seal = Mesh.Create.LoftRibs([innerLine.Points.ToArray(), outer.Points.ToArray()]);
        var frontSeal = seal.TransformedBy(posTrans);
        var backSeal = seal.Inverted().TransformedBy(negTrans);
        var tubus = Mesh.Create.LoftRibs([posOffset.Points.ToArray(), negOffset.Points.ToArray()]);
        Mesh<double> tmp = new([..frontSeal, ..tubus, ..backSeal]);
        return tmp;
    }