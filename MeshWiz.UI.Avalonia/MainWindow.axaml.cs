using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MeshWiz.Collections;
using MeshWiz.IO.Stl;
using MeshWiz.Kinematics;
using MeshWiz.Math;
using MeshWiz.OpenGL;
using MeshWiz.RefLinq;
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
        var start = surface.SweepCurve.Traverse(0.5);
        var dir = Vec3<double>.Create(0.8, 0.5, 0);

        var period = surface.TracePeriod(start,
            dir);
        var traces = period.TraceResult.Value;
        var lastCurve = traces[^1];
        var plane = new Plane<double>(surface.Axis.Direction, traces[0].Start);
        // var solve = Curve.Solver.IntersectionNewton(lastCurve, plane,AABB.From(0d,1d));
        // Console.WriteLine(solve.ToString());
        // var trace = sw.Elapsed;
        var jaggedGeodesic = period.FinalizedPath.Value;

        var poses = period.CreatePattern(20, useParallel: true).Value;
        meshi = surface.Tessellate(256).To<float>();


        LineViewWrapper.Unwrap.Polyline = jaggedGeodesic.To<Vec3<float>, float>();
        LineViewWrapper.Unwrap.Show = true;
        var mesh = meshi;
        this.MeshViewWrap.Unwrap.Mesh = mesh;
        var distance = mesh.BBox.Min.DistanceTo(mesh.BBox.Max) * 2;
        camera.Distance = 0.5f;
        var pC = poses[^1].EndPose.Position.To<float>();
        var d = Vec3<float>.Create(0, 0, pC.Z);
        Console.WriteLine($"{d} {pC} {d.DistanceTo(pC)} {surface.Axis.Direction}");
        var circ = new Circle3<float>(d, surface.Axis.Direction.To<float>(), d.DistanceTo(pC));
        camera.LookAt = mesh.VertexCentroid;
        BBoxWrap.Unwrap.BBox = mesh.BBox;
        var minY = mesh.BBox.Min.Y + 0.0001f;
        var maxY = mesh.BBox.Max.Y;
        var range = maxY - minY;
        poses = new PosePolyline<Pose3<double>, Vec3<double>, double>(
            Polyline.Reduction.DouglasPeucker<Pose3<double>, Vec3<double>, double>(poses.Poses,
                surface.RadiusRange.Max * 0.001));
        // var floatPoses = poses.Poses.ToArray().Select(p => p.To<float>()).ToArray();
        camera.UnitUp = Vec3<float>.UnitX;
        // var loft = Mesh.Create.Loft(floatPoses, 0.05f);

        _kinematicsView = new()
        {
            Camera = camera,
            Model = Mat4x4<float>.Identity,
            Show = true,
            Polyline = Polyline<Vec3<float>, float>.Empty
        };

        _kinmatX = new()
        {
            Camera = camera,
            Lines = [],
            Color = Color4.Red,
            Show = true,
            Model = Mat4x4<float>.Identity
        };
        _kinmatY = new()
        {
            Camera = camera,
            Lines = [],
            Color = Color4.Green,
            Show = true,
            Model = Mat4x4<float>.Identity
        };
        _kinmatZ = new()
        {
            Camera = camera,
            Lines = [],
            Color = Color4.Blue,
            Show = true,
            Model = Mat4x4<float>.Identity
        };

        GlParent.Children.AddRange(
            [
                _kinematicsView,
                _kinmatX,
                _kinmatY,
                _kinmatZ
            ]
        );
        MeshViewWrap.Show = false;
        LineViewWrapper.Show = false;
        MeshViewWrap.Unwrap.SolidColor = Color4.DarkViolet;
        var axisCount = 10;
        var axes = new IAxis[axisCount];
        camera.UnitUp = Vec3<float>.UnitZ;

        for (var i = 0; i < axisCount; i++)
        {
            var axisRange = AABB<double>.From(-double.Pi, double.Pi);
            var axisAxis = Ray3<double>.UnitX;
            var axisConnector = Pose3<double>.Identity.TranslateBy(Vec3<double>.UnitZ/(i+1));
            axes[i] = new RotationalAxis(axisRange, axisAxis, axisConnector,true);
        }
        
        // var traj = Pose3<double>.Identity.LineTo(Pose3<double>.Identity.TranslateBy(Vec3<double>.UnitZ).RotateAbout(Ray3<double>.UnitZ, Angle<double>.FromDegrees(90)));
        // axes[1]=new TrajectorialAxis(AABB.From(0.0, 1), new PosePolyline<Pose3<double>, Vec3<double>, double>(traj.StartPose,traj.EndPose,traj.StartPose));
        
        _kinematics = new KinematicChainState(new KinematicChain(axes));
        _kinematics.UpdateState(0,Iterator.Repeat(0.0, axes.Length).ToArray());
        UpdateKinmatPts(_kinematicsView!, _kinematics);
    }

    private MeshWiz.OpenGL.IndexedLineView _kinmatX;
    private MeshWiz.OpenGL.IndexedLineView _kinmatY;
    private MeshWiz.OpenGL.IndexedLineView _kinmatZ;

    public void AdvanceKinmatChain(KinematicChainState state, int num)
    {
        var stateVals = state.State;
        Span<double> newState = stackalloc double[stateVals.Length];
        for (var i = 0; i < state.Chain.Count; i++)
        {
            var step= state.Chain[i].Range.Size / 400*num;
            newState[i]=(stateVals[i] + step);
        }

        state.UpdateState(0, newState);
    }

    public void UpdateKinmatPts(LineView lv, KinematicChainState state)
    {
        var pts = state.AbsoluteConnectors.Select(p => p.Origin.To<float>()).Prepend(Vec3<float>.Zero).ToArray();
        lv.Polyline = new Polyline<Vec3<float>, float>(pts);
        _kinmatX.Lines = state.AbsoluteConnectors.Select(p => p.To<float>())
            .Select(p => p.Origin.LineTo(p.Origin + p.X * 0.1f)).ToArray();
        _kinmatY.Lines = state.AbsoluteConnectors.Select(p => p.To<float>())
            .Select(p => p.Origin.LineTo(p.Origin + p.Y * 0.1f)).ToArray();
        _kinmatZ.Lines = state.AbsoluteConnectors.Select(p => p.To<float>())
            .Select(p => p.Origin.LineTo(p.Origin + p.Z * 0.1f)).ToArray();
    }

    private LineView _kinematicsView;
    private KinematicChainState _kinematics;

    public static RotationalSurface<double> CreateTabloidSurface()
    {
        var c = new Circle2<double>(Vec2<double>.Zero, 1f);
        Arc2<double> arc = new(c, 0f, double.Pi);
        var seq = Iterator.Sequence(0.5, -0.001, -0.001);
        var v1 = seq.Select(arc.Traverse).ToArray();
        var shift = arc.Traverse(1) - arc.Traverse(0);

        var v2 = seq.Select(pos => pos + 0.5f).Select(arc.Traverse).Select(v => v + shift);

        Vec2<double>[] sweep = [..v2, ..v1];
        var surface =
            new RotationalSurface<double>(Vec3<double>.Zero.RayThrough(Vec3<double>.UnitX), sweep);
        return surface;
    }

    public static (double t, (double overlap, int pattern) period, RotationalSurface<double>.PeriodicalInfo info)[]
        CreateTable(RotationalSurface<double> surface)
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
        var items = Enumerable.Sequence(0.0, 1.00000001, 0.001).AsParallel().Select(t => (t, poseLine.GetPose(t)))
            .Select(pose =>
                (t: pose.t, period: surface.TracePeriod(pose.Item2.Origin, pose.Item2.Front)))
            .Select(pos => (t: pos.t, period: pos.period.CalculateOverlap(0.05).OrElse((
                1.0, 0)), info: pos.period)).ToArray();
        Array.Sort(items, (a, b) => a.t.CompareTo(b.t));
        foreach (var valueTuple in items) stream.WriteLine($"{valueTuple.t:F4},{valueTuple.period.Item2:F3}");
        return items;
    }

    public static RotationalSurface<double> GetSurface()
    {
        var file = "/home/finnimon/Downloads/liner_thickened.stl";
        var mesh = SafeStlReader<double>.Read(File.OpenRead(file)).Indexed();
        mesh = Mesh.Indexing.Split(mesh).OrderByDescending(m => m.BBox.GetVolume()).First();
        var up = Vec3<double>.UnitX;
        var vertices = mesh.Vertices.Iterate().ToArray(); //defensive copy
        Ray3<double> ray = new(mesh.VertexCentroid, up);
        up = ray.Direction; //normalized
        Line<Vec3<double>, double> rayline = ray;
        vertices = vertices.OrderBy(v =>
                rayline.GetClosestPositions(v).closest
            ).Iterate()
            .DistinctBy(v =>
                double.Round(rayline.GetClosestPositions(v).closest, 5))
            .ToArray();
        var sweep = new Vec2<double>[vertices.Length];
        for (var i = 0; i < vertices.Length; i++)
        {
            var p = vertices[i];
            var t = rayline.GetClosestPositions(p).closest;
            var x = t;
            var y = rayline.Traverse(t).DistanceTo(p);
            sweep[i] = new Vec2<double>(x, y) * 4;
        }

        sweep = Polyline.Reduction.DouglasPeucker<Vec2<double>, double>(new(sweep), 0.0001)
            .Points.ToArray();
        return new RotationalSurface<double>(ray, sweep);
    }

    private int dir = 1;

    /// <inheritdoc />
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        e.Handled = true;
        if (e.Key == Key.D0)
        {
            dir = -dir;
            Console.WriteLine($"Dir\t{dir}");
        }

        var num = e.Key switch
        {
            Key.D1 => 1,
            Key.D2 => 2,
            Key.D3 => 3,
            Key.D4 => 4,
            Key.D5 => 5,
            Key.D6 => 6,
            Key.D7 => 7,
            Key.D8 => 8,
            Key.D9 => 9,
            _ => -1
        };
        if (num == -1) return;
        AdvanceKinmatChain(_kinematics, num * dir);
        UpdateKinmatPts(_kinematicsView!, _kinematics);
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