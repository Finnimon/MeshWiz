using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MeshWiz.OpenTK;
using MeshWiz.Collections;
using MeshWiz.IO.Stl;
using MeshWiz.Math;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;
using OpenTK.Mathematics;

namespace MeshWiz.UI.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var lv2 = LineViewWrapper.Unwrap!;
        var camera = (OrbitCamera)OrbitCamera.Default();
        lv2.Camera = camera;
        // camera.MoveUp(0.2f);
        MeshViewWrap.Unwrap.Camera = camera;
        BBoxWrap.Unwrap.Camera = camera;
        camera.UnitUp = Vec3<float>.UnitZ;
        MeshViewWrap.Unwrap.LightColor = Color4.White;
        MeshViewWrap.Unwrap.SolidColor = Color4.DarkGray;
        MeshViewWrap.Show = true;
        MeshViewWrap.Unwrap.RenderModeFlags = RenderMode.Solid;
        MeshViewWrap.Unwrap.SolidColor = Color4.DarkSlateGray;
        MeshViewWrap.Unwrap.WireFrameColor = Color4.LightBlue;
        var bg = BgWrapper.Unwrap;
        bg.From = new Color4(0.1f, 0.1f, 0.1f, 1f);
        bg.From = Color4.DarkSlateBlue;
        bg.To = bg.From;
        bg.RotationMillis = 100000;

        LineViewWrapper.Unwrap.LineWidth = 2.5f;
        LineViewWrapper.Unwrap.Show = true;

        IIndexedMesh<float> meshi = AABB.From(Vec3<float>.NegativeOne, Vec3<float>.One)
            .Tessellate()
            .Indexed();
        //
        var surface = CreateTabloidSurface();
        var table= CreateTable(surface);
        // surface = GetSurface();
        
        
        // surface = Create("/home/finnimon/Downloads/liner_thickened.stl");
        // camera.UnitUp = surface.Axis.Direction.To<float>();
        
        // var surface = JaggedRotationalSurface<float>.FromSweepCurve(arcPoly, ray);
        Polyline<Vec3<double>, double> jaggedGeodesic;

        // jaggedGeodesic = surface.TraceGeodesicCycles(new(1,0,surface.SweepCurve.Traverse(0.5f).Z),
        //     Vec3<double>.UnitY + Vec3<double>.UnitZ,
        //     10000);

        // jaggedGeodesic = surface.TraceFullCycle(new(1, 0, surface.SweepCurve.Traverse(0.5f).Z),
        //     new(0, 1, 1));
        // jaggedGeodesic = surface.TraceFullCycle(new(1, 0, surface.SweepCurve.Traverse(0.5f).Z),
        //     new(0, 1, 1));
        // jaggedGeodesic = surface.TraceGeodesicCycles(new(1,0,surface.SweepCurve.Traverse(0.5f).Z),
        //     new(0,1,1),
        //     childSurfaceCount:1200);
        // var sum = ParallelEnumerable.Range(1, 1000).Select(z => surface.TracePeriod(
        //     new Vec3<double>(1, 0, surface.SweepCurve.Traverse(0.5f).Z),
        //     new Vec3<double>(0, 1, -1 - z * 0.1)).FinalizedPath).Where(p => p).Sum(p => p.Value.Count);
        var sw = Stopwatch.StartNew();
        // sum = ParallelEnumerable.Range(1, 1000).Select(z => surface.TracePeriod(
        //     new Vec3<double>(1, 0, surface.SweepCurve.Traverse(0.5f).Z),
        //     new Vec3<double>(0, 1, 1 + z * 0.1)).FinalizedPath).Where(p => p).Sum(p => p.Value.Count);
        // var el = sw.Elapsed;
        // sw.Restart();
        // sum = Enumerable.Range(1, 1000).Select(z => surface.TracePeriod(
        //     new Vec3<double>(1, 0, surface.SweepCurve.Traverse(0.5f).Z),
        //     new Vec3<double>(0, 1, 1 + z * 0.1)).FinalizedPath).Where(p => p).Sum(p => p.Value.Count);
        var elapsed = sw.Elapsed;
        // Console.WriteLine($"Par: {el} Seq: {elapsed}");
        sw.Restart(); //0x139
        var start = surface.SweepCurve.Traverse(0.5);
        var dir = new Vec3<double>(0.8, 0.5, 0);
        
        var period = surface.TracePeriod(start,
            dir);
        var context = table.Where(t=>t.info.Exit).MinBy(a => a.period.overlap);
        period = context.info;
        var traces = period.TraceResult.Value;
        var lastCurve = traces[^1];
        var plane = new Plane3<double>(surface.Axis.Direction, traces[0].Start);
        // var solve = Curve.Solver.IntersectionNewton(lastCurve, plane,AABB.From(0d,1d));
        // Console.WriteLine(solve.ToString());
        // var trace = sw.Elapsed;
        jaggedGeodesic = period.FinalizedPath;
        sw.Restart(); //0x139
        
        var poses = period.CreatePattern(context.period.pattern/2+4,useParallel:true).Value;
        // var reduced =
        //     Polyline.Reduction.DouglasPeucker<Pose3<double>, Vec3<double>, double>(poses.Poses,
        //         surface.RadiusRange.Max * 0.001);
        // Console.WriteLine($"Phase {period.Phase.Value}");
        // Console.WriteLine($"Trace {trace} GetPoses  {sw.Elapsed}");
        // poses = new(reduced);
        // sw.Restart();
        // jaggedGeodesic = poses.ToPolyline();
        // var normals = jaggedGeodesic.Points
        //     .ToArray()
        //     .Select(p => Line<Vec3<double>, double>.FromAxisVector(p, surface.NormalAt(p)))
        //     .Select(l => new Line<Vec3<float>, float>(l.Start.To<float>(), l.End.To<float>())).ToArray();
        Console.WriteLine(sw.Elapsed);
        meshi = surface.Tessellate(256).To<float>();
        
        
        LineViewWrapper.Unwrap.Polyline = jaggedGeodesic.To<Vec3<float>, float>();
        LineViewWrapper.Unwrap.Show = true;
        // var mesh = BvhMesh<float>.SurfaceAreaHeuristic(meshi);
        var mesh = meshi;
        this.MeshViewWrap.Unwrap.Mesh = mesh;
        var distance = mesh.BBox.Min.DistanceTo(mesh.BBox.Max) * 2;
        camera.Distance = 0.5f;
        var pC = poses[^1].EndPose.Position.To<float>();
        var d = new Vec3<float>(0, 0, pC.Z);
        Console.WriteLine($"{d} {pC} {d.DistanceTo(pC)} {surface.Axis.Direction}");
        var circ = new Circle3<float>(d, surface.Axis.Direction.To<float>(), d.DistanceTo(pC));
        camera.LookAt = mesh.VertexCentroid;
        BBoxWrap.Unwrap.BBox = mesh.BBox;
        var minY = mesh.BBox.Min.Y + 0.0001f;
        var maxY = mesh.BBox.Max.Y;
        var range = maxY - minY;
        poses = new PosePolyline<Pose3<double>, Vec3<double>, double>(Polyline.Reduction.DouglasPeucker<Pose3<double>, Vec3<double>, double>(poses.Poses,
            surface.RadiusRange.Max * 0.001));
        var floatPoses = poses.Poses.ToArray().Select(p => p.To<float>()).ToArray();
        camera.UnitUp=Vec3<float>.UnitX;
        var loft = Mesh.Create.Loft(floatPoses, 0.05f);
        GlParent.Children.AddRange(
            [
                new SmoothMeshView()
                {
                    Mesh = loft,
                    Camera = camera,
                    RenderModeFlags = RenderMode.Solid,
                    Show = true,
                    SolidColor = new(1,0,0,0.1f),
                    DepthOffset = 0.0005f
                },
                new LineView()
                {
                    Polyline = poses.ToPolyline().To<Vec3<float>, float>(),
                    Color=Color4.Pink,
                    Camera = camera,
                    DepthOffset = 0.0006f
                }
            ]
        );
        LineViewWrapper.Unwrap.Show = true;
        LineViewWrapper.Show = true;
        MeshViewWrap.Unwrap.SolidColor = Color4.DarkViolet;
    }

    public static RotationalSurface<double> CreateTabloidSurface()
    {
        var c = new Circle2<double>(Vec2<double>.Zero, 1f);
        Arc2<double> arc = new(c, 0f, double.Pi);
        var seq = Enumerable.Sequence(0.5, -0.001, -0.001);
        var v1 = seq.Select(arc.Traverse).ToArray();
        var shift = arc.Traverse(1) - arc.Traverse(0);

        var v2 = seq.Select(pos => pos + 0.5f).Select(arc.Traverse).Select(v => v + shift);

        Vec2<double>[] sweep = [..v2, ..v1];
        var surface =
            new RotationalSurface<double>(Vec3<double>.Zero.RayThrough(Vec3<double>.UnitX), sweep);
        return surface;
    }

    public static (double t, (double overlap, int pattern) period, RotationalSurface<double>.PeriodicalInfo info)[] CreateTable(RotationalSurface<double> surface)
    {
        var axis = surface.Axis;
        var up = axis.Direction;
        var mid = surface.SweepCurve.Traverse(0.5);
        var normal = surface.NormalAt(mid);
        var initialDir = Vec3<double>.Cross(up, normal);
        var startPose = Pose3<double>.CreateFromOrientation(mid, up, normal);
        var endPose = Pose3<double>.CreateFromOrientation(mid, initialDir, normal);
        PoseLine<Pose3<double>, Vec3<double>, double> poseLine = new(startPose, endPose);
        using var stream = new StreamWriter("/home/finnimon/Documents/output.csv");
        var items = Enumerable.Sequence(0.0, 1.00000001, 0.001).AsParallel().Select(t => (t, poseLine.GetPose(t))).Select(pose =>
            (t: pose.t, period: surface.TracePeriod(pose.Item2.Origin, pose.Item2.Front)))
            .Select(pos=> (t: pos.t,period:pos.period.CalculateOverlap(0.05).OrElse((
                1.0, 0)),info:pos.period)).ToArray();
        Array.Sort(items,(a,b)=>a.t.CompareTo(b.t));
        foreach (var valueTuple in items) stream.WriteLine($"{valueTuple.t:F4},{valueTuple.period.Item2:F3}");
        return items;
    }

    public static RotationalSurface<double> GetSurface()
    {
        var file = "/home/finnimon/Downloads/liner_thickened.stl";
        var mesh = SafeStlReader<double>.Read(File.OpenRead(file)).Indexed();
        mesh = Mesh.Indexing.Split(mesh).OrderByDescending(m => m.BBox.GetVolume()).First();
        var up=Vec3<double>.UnitX;
        var vertices= mesh.Vertices.ToArray();//defensive copy
        Ray3<double> ray = new(mesh.VertexCentroid, up);
        up = ray.Direction;//normalized
        Line<Vec3<double>,double> rayline = ray;
        vertices= vertices.OrderBy(v =>
            rayline.GetClosestPositions(v).closest
        ).DistinctBy(v =>
            double.Round(rayline.GetClosestPositions(v).closest,5))
            .ToArray();
        var sweep =new Vec2<double>[vertices.Length];
        for (var i = 0; i < vertices.Length; i++)
        {
            var p = vertices[i];
            var t = rayline.GetClosestPositions(p).closest;
            var x = t;
            var y = rayline.Traverse(t).DistanceTo(p);
            sweep[i] = new Vec2<double>(x, y)*4;
        }

        sweep = Polyline.Reduction.DouglasPeucker<Vec2<double>, double>(new(sweep),0.0001)
            .Points.ToArray();
        return new RotationalSurface<double>(ray, sweep);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e) =>
        MeshViewWrap.Unwrap.Camera.MoveForwards((float)e.Delta.Y);

    private bool _isPressed;

    private Point _pointerPos;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _isPressed = true;
        _pointerPos = e.GetPosition(this);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
        => _isPressed = false;

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e) => _isPressed = false;
    protected override void OnPointerExited(PointerEventArgs e) => _isPressed = false;


    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_isPressed) return;
        var d = e.GetPosition(this) - _pointerPos;

        if (e.Properties.IsLeftButtonPressed)
        {
            MeshViewWrap.Unwrap.Camera.MoveRight(-(float)d.X / 400);
            MeshViewWrap.Unwrap.Camera.MoveUp((float)d.Y / 400);
            _pointerPos = e.GetPosition(this);
            return;
        }

        MeshViewWrap.Unwrap.Camera.LookRight((float)d.X / 1000);
        MeshViewWrap.Unwrap.Camera.LookUp((float)d.Y / 1000);
        _pointerPos = e.GetPosition(this);
    }

    private void UpdateBlocks(IndexedMesh<float> mesh)
    {
        var centroid = mesh.VertexCentroid;
        var surfCentraid = mesh.SurfaceCentroid;
        var voluCentroid = mesh.VolumeCentroid;
        var volume = mesh.Volume;
        var area = mesh.SurfaceArea;
        CentroidBlock.Text =
            $"Centroids: Vertex [{centroid.X:N2}|{centroid.Y:N2}|{centroid.Z:N2}] Area [{surfCentraid.X:N2}|{surfCentraid.Y:N2}|{surfCentraid.Z:N2}] Volume [{voluCentroid.X:N2}|{voluCentroid.Y:N2}|{voluCentroid.Z:N2}] ";
        VolumeBlock.Text = $"Volume: {volume:N2}";
        SurfBlock.Text = $"Surface area: {area:N2}";
    }

    private void SetRenderMode(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menu) return;
        e.Handled = true;
        MeshViewWrap.Unwrap.RenderModeFlags = menu.Header switch
        {
            nameof(Wireframe) => RenderMode.Wireframe,
            nameof(Solid) => RenderMode.Solid,
            nameof(WireframedSolid) => RenderMode.Wireframe | RenderMode.Solid,
            nameof(None) => RenderMode.None,
            _ => RenderMode.Solid
        };
    }

    private void ExportBinary(object? sender, RoutedEventArgs e)
        => Task.Run(() => ExportStlCommand(e, false));

    private void ExportAscii(object? sender, RoutedEventArgs e)
        => Task.Run(() => ExportStlCommand(e, true));

    private async Task ExportStlCommand(RoutedEventArgs routedEventArgs, bool ascii)
    {
        routedEventArgs.Handled = true;

        var topLevel = GetTopLevel(this);
        var storageProvider = topLevel?.StorageProvider ?? throw new PlatformNotSupportedException();

        var options = new FilePickerSaveOptions()
        {
            DefaultExtension = ".stl",
            SuggestedFileName = "model.stl"
        };
        var fileSaver = await storageProvider.SaveFilePickerAsync(options);
        if (fileSaver is null) return;
        try
        {
            var mesh = MeshViewWrap.Unwrap.Mesh;
            await fileSaver.DeleteAsync();
            await using var writeStream = await fileSaver.OpenWriteAsync();
            await Task.Run(() =>
            {
                if (ascii) AsciiStlWriter<float>.Write(mesh, writeStream, leaveOpen: true);
                else FastBinaryStlWriter.Write(mesh, writeStream, leaveOpen: true);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }


    private void ColorSettingsChanged(object? sender, ColorChangedEventArgs e)
    {
        var col = new Color4(e.NewColor.R, e.NewColor.G, e.NewColor.B, e.NewColor.A);
        if (WireframeColorPicker.Equals(sender))
            MeshViewWrap.Unwrap.WireFrameColor = col;
        else if (SolidColorPicker.Equals(sender))
            MeshViewWrap.Unwrap.SolidColor = col;
    }


    public async void LoadMesh(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;

        var topLevel = GetTopLevel(this);
        var storageProvider = topLevel?.StorageProvider ?? throw new PlatformNotSupportedException();

        var options = new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("STL")],
            SuggestedFileName = "model.stl",
            Title = "Select an STL file"
        };

        var result = await storageProvider.OpenFilePickerAsync(options);
        if (result is not { Count: > 0 }) return;

        var fileExt = Path.GetExtension(result[0].Path.AbsolutePath);
        IndexedMesh<float> mesh;
        try
        {
            await using var stream = await result[0].OpenReadAsync();
            var ifMesh = fileExt switch
            {
                ".stl" => FastStlReader.Read(stream, true),
                _ => throw new NotSupportedException()
            };
            mesh = new(ifMesh);
            await mesh.InitializeAsync();
            MeshViewWrap.Unwrap.Mesh = mesh;
            MeshViewWrap.Unwrap.Camera.LookAt = mesh.VolumeCentroid;
            BBoxWrap.Unwrap.BBox = mesh.BBox;
            UpdateBlocks(mesh);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}