using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using MathNet.Numerics.LinearAlgebra.Solvers;
using MeshWiz.Buffers;
using MeshWiz.Collections;
using MeshWiz.CompilerServices;
using MeshWiz.IO.Stl;
using MeshWiz.Math;
using MeshWiz.RefLinq;
using MeshWiz.Utility;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

int[] arr = new int[4];
using var poolBuf=Pool.Rent<int>(4);
using var buf = Freelist.Shared.Rent<int>(5);
using var strBuf = Freelist.Shared.Rent<int>(19);

InlineRefArray2<Freelist.Buffer<int>> inline = default;
inline[0] = buf;
inline[1] = strBuf;
foreach (var buffer in inline)
{
    Console.WriteLine(buffer.Span.Length);
}


return;
Console.WriteLine("Hello World!");
var pl = new Circle3<double>(default, Vec3<double>.UnitZ, 1)
    .GetGeodesicFromEntry(Vec3<double>.UnitX, -Vec3<double>.UnitX).ToPosePolyline();
var str = JsonSerializer.Serialize(pl);
Console.WriteLine(str);
var pl2 = JsonSerializer.Deserialize<PosePolyline<Pose3<double>, Vec3<double>, double>>(str)!;
Console.WriteLine(pl2.Equals(pl));

// Console.WriteLine($"Total intersection time {sw.Elapsed} for lines {buf.Count}");
static (IMesh<double> mesh, List<Polyline<Vec3<double>, double>> layers) DoStuff()
{
    // var halfCyl = CreateHalfCyl().Indexed();
    var sw = Stopwatch.StartNew();
    const string benchyFile = "/home/finnimon/Downloads/3DBenchy.stl";
    var srcMesh = SafeStlReader<double>.Read(System.IO.File.OpenRead(benchyFile));
    Console.WriteLine($"load time {sw.Elapsed} for {new FileInfo(benchyFile).Length:N} bytes");
    sw.Restart();
    var bvh = Bvh.Mesh<double>.Sah(srcMesh);
    Console.WriteLine($"create bvh time {sw.Elapsed}");
    var longestAxisX = bvh.BBox.Size.X > bvh.BBox.Size.Y;
    var intersection = new Stopwatch();
    var unify = new Stopwatch();
    var normal = (Vec3<double>.UnitZ + (longestAxisX ? Vec3<double>.UnitX : Vec3<double>.UnitY)).Normalized();
    var plane = new Plane<double>(normal, 0);
    var start = plane.SignedDistance(bvh.BBox.Min);
    var end = plane.SignedDistance(bvh.BBox.Max);
    var allLayers = new List<Polyline<Vec3<double>, double>>();
    var allLayers2 = allLayers.ToList();
    const int layerCount = 10000;
    var buf = new RollingList<Line<Vec2<double>, double>>();
    Stopwatch unify2 = new();
    for (var i = 0; i <= layerCount; i++)
    {
        var pt = Vec3<double>.Lerp(bvh.BBox.Min, bvh.BBox.Max, ((double)i) / (double)layerCount);
        var lplane = new Plane<double>(plane.Normal, pt);
        intersection.Start();
        buf.Clear();
        var pls = bvh.Intersect(lplane, buf);
        var pls2 = pls.ToArray();
        intersection.Stop();
        unify.Start();
        var polys = Polyline.Creation.UnifyNonReversing(pls).Select(lplane.ProjectIntoWorld).ToArray();
        allLayers.AddRange(polys);
        unify.Stop();
        unify2.Start();
        var polys2 = Polyline.Creation.UnifyByDictionary(pls2).Select(lplane.ProjectIntoWorld).ToArray();
        allLayers2.AddRange(polys2);
        unify2.Stop();
    }

    Console.WriteLine($"tri count {bvh.Count}");
    Console.WriteLine($"layer count {layerCount}");
    Console.WriteLine($"intersection time {intersection.Elapsed}");
    Console.WriteLine($"unfiy time {unify.Elapsed} VS {unify2.Elapsed}");
    Console.WriteLine($"avg seg count {allLayers.Iterate().Average(l => l.Count)}");
    Console.WriteLine($"avg seg count {allLayers2.Iterate().Average(l => l.Count)}");
    Console.WriteLine(
        $"Closed VS non closed loops {allLayers.Iterate().Count(l => l.IsClosed)} {allLayers.Iterate().Count(l => !l.IsClosed)}");
    Console.WriteLine(
        $"Closed VS non closed loops {allLayers2.Iterate().Count(l => l.IsClosed)} {allLayers2.Iterate().Count(l => !l.IsClosed)}");
    return (bvh, allLayers);
}

static Mesh<double> CreateHalfCyl()
{
    var arc = new Arc3<double>(new Circle3<double>(default, Vec3<double>.UnitX, 1.0), -0.5 * double.Pi,
        0.5 * double.Pi);
    var outer = arc.ToPolyline(new PolylineTessellationParameter<double> { MaxAngularDeviation = 0.005 });
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