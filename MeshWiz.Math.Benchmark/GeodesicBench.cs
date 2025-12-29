using System.Numerics;
using BenchmarkDotNet.Attributes;
using MeshWiz.IO.Stl;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math.Benchmark;

[MemoryDiagnoser]
public class GeodesicBench<TNum>
where TNum:unmanaged,IFloatingPointIeee754<TNum>
{
    public RotationalSurface<TNum>? Surface;
    private Vec3<TNum> Start,Dir;
    // [Params(0.01, 0.1)] public double Width;
    private RotationalSurface<TNum>.PeriodicalInfo? _periodicalInfo;
    [GlobalSetup]
    public void Setup()
    {
        Surface = GetSurface();
        Start = Surface.SweepCurve.Traverse(Numbers<TNum>.Half);
        Dir = new Vec3<double>(0.8, 0.5, 0).To<TNum>();
        _periodicalInfo = TracePeriod();
    }
    private static RotationalSurface<TNum> GetSurface()
    {
        var file = "/home/finnimon/Downloads/liner_thickened.stl";
        var mesh = SafeStlReader<TNum>.Read(File.OpenRead(file)).Indexed();
        mesh = Mesh.Indexing.Split(mesh).OrderByDescending(m => m.BBox.GetVolume()).First();
        var up=Vec3<TNum>.UnitX;
        var vertices= mesh.Vertices.ToArray();//defensive copy
        Ray3<TNum> ray = new(mesh.VertexCentroid, up);
        up = ray.Direction;//normalized
        Line<Vec3<TNum>,TNum> rayline = ray;
        vertices= vertices.OrderBy(v =>
                rayline.GetClosestPositions(v).closest
            ).DistinctBy(v =>
                TNum.Round(rayline.GetClosestPositions(v).closest,5))
            .ToArray();
        var sweep =new Vec2<TNum>[vertices.Length];
        for (var i = 0; i < vertices.Length; i++)
        {
            var p = vertices[i];
            var t = rayline.GetClosestPositions(p).closest;
            var x = t;
            var y = rayline.Traverse(t).DistanceTo(p);
            sweep[i] = new Vec2<TNum>(x, y);
        }

        sweep = Polyline.Reduction.DouglasPeucker<Vec2<TNum>, TNum>(new(sweep),Numbers<TNum>.ZeroEpsilon)
            .Points.ToArray();
        return new RotationalSurface<TNum>(ray, sweep);
    }
    // [Benchmark]
    // public Angle<TNum> GetPhase() => TracePeriod().Phase;
    public RotationalSurface<TNum>.PeriodicalInfo TracePeriod() => Surface!.TracePeriod(Start, Dir);
    //
    [Benchmark]
    public PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum> FinalizedPoses() => TracePeriod().FinalizedPoses;

    [Benchmark]
    public PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum> FinalizedPosesOnly() => CopyPeriodicalInfo()!.FinalizedPoses;
    // [Benchmark]
    // public Polyline<Vec3<TNum>, TNum> FinalizedPath() => TracePeriod().FinalizedPath;
    [Benchmark(Baseline = true)]
    public RotationalSurface<TNum>.PeriodicalInfo TracePeriodOnly() => TracePeriod();
    //
    // private static TNum _overlap;
    // [Benchmark]
    // public TNum CalcOverlapOnly()
    // {
    //     TNum overlap= _periodicalInfo!.CalculateOverlap(TNum.CreateTruncating(Width));
    //     // Console.WriteLine($"Overlap : {overlap}");
    //     _overlap = overlap;
    //     return overlap;
    // }

    // [Benchmark(Baseline = true)]
    // public Ray3<TNum> OldExit()
    // {
    //     var period = CopyPeriodicalInfo();
    //     return period.Exit;
    // }
    //
    // [Benchmark]
    // public Ray3<TNum> ExitNewton()
    // {
    //     return CopyPeriodicalInfo().Exit2();
    // }
    //
    // [Benchmark]
    // public Ray3<TNum> ExitBinary()
    // {
    //     return CopyPeriodicalInfo().Exit3();
    // }

    private RotationalSurface<TNum>.PeriodicalInfo CopyPeriodicalInfo()
    {
        var (startingConditions, ray3, traceResult) = _periodicalInfo!;
        RotationalSurface<TNum>.PeriodicalInfo period = new(startingConditions, ray3, traceResult);
        return period;
    }
}