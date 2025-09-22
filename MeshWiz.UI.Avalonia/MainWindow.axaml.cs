using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MeshWiz.Abstraction.Avalonia;
using MeshWiz.Abstraction.OpenTK;
using MeshWiz.IO;
using MeshWiz.IO.Stl;
using MeshWiz.Math;
using MeshWiz.Slicer;
using MeshWiz.Slicer.OpenTK;
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
        IIndexedMesh<float> meshi = AABB.Tessellate(box).Indexed();
        // meshi = new Sphere<float>(Vector3<float>.Zero, 10).Tessellate().Indexed();

        // meshi =new IndexedMesh3<float>(new Sphere<float>(Vector3<float>.Zero, 1).TessellatedSurface);
        meshi = MeshIO.ReadFile<FastStlReader, float>("/home/finnimon/source/repos/TestFiles/drag.stl").Indexed();
        var mesh = new BvhMesh<float>(meshi);
        // mesh=new  IndexedMesh3<float>(new Sphere<float>(Vector3<float>.Zero, 1).TessellatedSurface);
        Console.WriteLine(
            $"Tri count: {mesh.Count}, Effec vert count: {mesh.Count * 3}, Indexed vert count: {mesh.Vertices.Length}");
        var distance = mesh.BBox.Min.DistanceTo(mesh.BBox.Max) * 2;
        camera.Distance = 0.5f;
        camera.LookAt = mesh.SurfaceCentroid;
        MeshViewWrap.Unwrap.Mesh = mesh;
        BBoxWrap.Unwrap.Camera = camera;
        BBoxWrap.Unwrap.BBox = mesh.BBox;
        var minY = mesh.BBox.Min.Y + 0.0001f;
        var maxY = mesh.BBox.Max.Y;
        var range = maxY - minY;

        var layerCount = 64;
        var levelHeight = range / layerCount;
        var pathWidth = levelHeight;
        var epsilon = range / 10000;

        Stopwatch sw = Stopwatch.StartNew();
        var concentricPattern = ParallelEnumerable.Range(1, layerCount)
            .Select(i => minY + levelHeight * i+0.5f*levelHeight)
            .Select(h => new Plane3<float>(Vector3<float>.UnitY, Vector3<float>.UnitY * h))
            .Select(plane => (intersected: mesh.IntersectRolling(plane), plane))
            .SelectMany(tuple => tuple.intersected.Select(polyline => (polyline, tuple.plane)))
            // .Select(tuple => tuple with { polyline = Polyline.Reduction.DouglasPeucker(tuple.polyline, epsilon) })
            .Select(tuple => (pattern: SimpleConcentric.GenPattern(tuple.polyline, pathWidth), tuple.plane))
            .SelectMany(tuple => tuple.pattern.Select(tuple.plane.ProjectIntoWorld))
            .ToArray();
        // var concentricPatternTcp = ParallelEnumerable.Range(1, layerCount)
        //     .Select(i => minY + levelHeight * i - levelHeight / 2)
        //     .Select(h => new Plane3<float>(Vector3<float>.UnitY, Vector3<float>.UnitY * h))
        //     .Select(plane => SimpleConcentric.GenLayer(plane, mesh, pathWidth)).ToArray();
        sw.Stop();

        Console.WriteLine(
            $"In {sw.Elapsed.TotalSeconds:F3} Total line count: {concentricPattern.Sum(layer => layer.Count)}");

        var count = 0;
        // for (var h = minY; h <= maxY; h += levelHeight)
        // {
        //     var plane = new Plane3<float>(Vector3<float>.UnitY, Vector3<float>.UnitY * h);
        //     var pl = mesh.IntersectRolling(plane);
        //     foreach (var polyline in pl)
        //     {
        //         var reduced = Polyline.Reduction.DouglasPeucker(polyline, epsilon);
        //         var simplified = SimpleConcentric.GenPattern(reduced, pathWidth);
        //         foreach (var simple in simplified)
        //         {
        //             count++;
        //             var world = plane.ProjectIntoWorld(simple);
        //             polylines.AddRange(world);
        //         }
        //     }
        // }
        // polylines = selfCr3.ToList();
        MeshViewWrap.Show = true;
        MeshViewWrap.Unwrap.RenderModeFlags = RenderMode.None;
        // GlParent.Children.Add(
        //     new GLWrapper<ToolPathView>
        //     {
        //         Unwrap = new ToolPathView
        //         {
        //             Camera = camera,
        //             PerimeterColor = Color4.LawnGreen,
        //             Show = true,
        //             LineWidth = 1,
        //             Layers = concentricPatternTcp
        //         }
        //     }
        // );
        GlParent.Children.Add(new GLWrapper<IndexedLineView>
        {
            Unwrap = new IndexedLineView
            {
                Color = Color4.LawnGreen,
                Lines = concentricPattern.SelectMany(manyLine => manyLine),
                Camera = camera,
                LineWidth = 1,
                Show = true,
            }
        });


        camera.LookAt = mesh.VolumeCentroid;
        Console.WriteLine($"distance: {distance}");
        Console.WriteLine(mesh.BBox.Min);
        Console.WriteLine(mesh.BBox.Max);
        Console.WriteLine(mesh.BBox.CombineWith(Vector3<float>.Zero).Min);
        Console.WriteLine(mesh.Count);
        var bg = BgWrapper.Unwrap;
        bg.From = Color4.DarkBlue;
        bg.To = Color4.DarkRed;
        bg.RotationMillis = 100000;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e) => MeshViewWrap.Unwrap.Camera.MoveForwards((float)e.Delta.Y);

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

        var topLevel = TopLevel.GetTopLevel(this);
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