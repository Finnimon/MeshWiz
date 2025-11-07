using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MeshWiz.Abstraction.OpenTK;
using MeshWiz.IO.Stl;
using MeshWiz.Math;
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
        MeshViewWrap.Unwrap.LightColor = Color4.White;
        MeshViewWrap.Unwrap.SolidColor = Color4.DarkGray;
        camera.UnitUp = Vector3<float>.UnitY;
        var box = AABB<Vector3<float>>
            .From(new Vector3<float>(-10, -10, -10))
            .CombineWith(new Vector3<float>(10, 10, 10));
        IIndexedMesh<float> meshi = box.Tessellate().Indexed();
        Vector2<float>[] sweep = [new(0f,0f),new(1,1),new(2,1),new(3,0)];
        var surface=new JaggedRotationalSurface<float>(Vector3<float>.Zero.RayThrough(Vector3<float>.UnitX),sweep);
        // var surface = JaggedRotationalSurface<float>.FromSweepCurve(arcPoly, ray);
        var sw = Stopwatch.StartNew();
        var jaggedGeodesic = surface.TraceGeodesics(surface.SweepCurve.Traverse(0.5f),
            -Vector3<float>.UnitX + Vector3<float>.UnitY,
            cycleCount:1000);
        var newPoints = new Vector3<float>[jaggedGeodesic.Points.Length+1];
        jaggedGeodesic.Points.CopyTo(newPoints);
        newPoints[^1] = JaggedRotationalSurface<float>.TransFormedDir + newPoints[^2];
        jaggedGeodesic = new(newPoints);
        var cyl=surface.OfType<Cylinder<float>>().ToArray();
        Console.WriteLine($"Num cyl {cyl.Length}");
        Console.WriteLine($"Geodesicjagged {sw.Elapsed}");
        LineViewWrapper.Unwrap.Polyline= jaggedGeodesic;
        LineViewWrapper.Unwrap.LineWidth = 2.5f;
        LineViewWrapper.Unwrap.Show = true;
        sw.Restart();
        meshi = surface.Tessellate(256);
        Console.WriteLine($"Tessellate {sw.Elapsed.TotalMilliseconds}ms");
        var mesh = BvhMesh<float>.SurfaceAreaHeuristic(meshi);
        var distance = mesh.BBox.Min.DistanceTo(mesh.BBox.Max) * 2;
        camera.Distance = 0.5f;
        camera.LookAt = mesh.VertexCentroid;
        MeshViewWrap.Unwrap.Mesh = meshi;
        BBoxWrap.Unwrap.Camera = camera;
        BBoxWrap.Unwrap.BBox = mesh.BBox;
        var minY = mesh.BBox.Min.Y + 0.0001f;
        var maxY = mesh.BBox.Max.Y;
        var range = maxY - minY;
        MeshViewWrap.Show = true;
        MeshViewWrap.Unwrap.RenderModeFlags = RenderMode.Solid;
        MeshViewWrap.Unwrap.SolidColor = Color4.DarkSlateGray;
        MeshViewWrap.Unwrap.WireFrameColor = Color4.LightBlue;
        var bg = BgWrapper.Unwrap;
        bg.From = new(0.1f,0.1f,0.1f,1f);
        bg.From = Color4.DarkSlateBlue;
        bg.To = bg.From;
        bg.RotationMillis = 100000;
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
        => ExportStlCommand(e, false);

    private void ExportAscii(object? sender, RoutedEventArgs e)
        => ExportStlCommand(e, true);

    private async void ExportStlCommand(RoutedEventArgs routedEventArgs, bool ascii)
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