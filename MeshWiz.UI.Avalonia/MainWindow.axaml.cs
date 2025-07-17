using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MeshWiz.Abstraction.OpenTK;
using MeshWiz.IO;
using MeshWiz.IO.Stl;
using MeshWiz.Math;
using OpenTK.Mathematics;

namespace MeshWiz.UI.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var lv2 = LineViewWrapper.Unwrap;
        lv2.PolyLine = new PolyLine<Vector3<float>, float>([
            new(20,20,0),
            new(-20,20,0),
            new(-20,-20,0),
            new(20,-20,0),
            new(20,20,0),
        ]);
        var camera = (OrbitCamera) OrbitCamera.Default();
        lv2.Camera=camera;
        // camera.MoveUp(0.2f);
        MeshViewWrap.Unwrap.Camera = camera;
        MeshViewWrap.Unwrap.LightColor=Color4.White;
        MeshViewWrap.Unwrap.SolidColor=Color4.DarkGray;
        camera.UnitUp=Vector3<float>.UnitZ;
        var tesselations=BBox3<float>
            .NegativeInfinity
            .CombineWith(new Vector3<float>(-10,-10,-10))
            .CombineWith(new Vector3<float>(10,10,10))
            .TessellatedSurface;
        var tessellations2=BBox3<float>
            .NegativeInfinity
            .CombineWith(new Vector3<float>(-1,-2,-1))
            .CombineWith(new Vector3<float>(1,0,1))
            .TessellatedSurface;
        var mesh = new IndexedMesh3<float>(tesselations);
        // mesh =new IndexedMesh3<float>(new Sphere<float>(Vector3<float>.Zero, 1).TessellatedSurface);
        mesh= IMeshReader<float>.ReadFile<FastStlReader>("/home/finnimon/source/repos/TestFiles/artillery-witch.stl").Indexed();
        mesh = MeshSplitter.Split(mesh)[1];
        mesh.Shift(-mesh.VolumeCentroid);
        // mesh=new  IndexedMesh3<float>(new Sphere<float>(Vector3<float>.Zero, 1).TessellatedSurface);
        Console.WriteLine($"Tri count: {mesh.Count}, Effec vert count: {mesh.Count*3}, Indexed vert count: {mesh.Vertices.Length}");
        var distance=mesh.BBox.Min.Distance(mesh.BBox.Max)*2;
        camera.Distance = 0.5f;
        camera.LookAt=mesh.Centroid;
        MeshViewWrap.Unwrap.Mesh = mesh;
        BBoxWrap.Unwrap.Camera=camera;
        BBoxWrap.Unwrap.BBox=mesh.BBox;
        
        // SmoothMeshViewWrapper.Wrapped.Camera=camera;
        // SmoothMeshViewWrapper.Wrapped.Mesh=new IndexedMesh3<float>(tessellations2);
        //
        camera.LookAt=mesh.Centroid;
        Console.WriteLine($"distance: {distance}");
        Console.WriteLine(mesh.BBox.Min);
        Console.WriteLine(mesh.BBox.Max);
        Console.WriteLine(mesh.BBox.CombineWith(Vector3<float>.Zero).Min);
        Console.WriteLine(mesh.Count);
        var bg= BgWrapper.Unwrap;
        bg.From=Color4.DarkBlue;
        bg.To=Color4.DarkRed;
        bg.RotationMillis = 100000;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        MeshViewWrap.Unwrap.Camera.MoveForwards((float) e.Delta.Y);
    }

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
            var d = _pointerPos - e.GetPosition(this);
            
        if(e.Properties.IsLeftButtonPressed)
        {
            MeshViewWrap.Unwrap.Camera.MoveRight((float)d.X / 400);
            MeshViewWrap.Unwrap.Camera.MoveUp(-(float)d.Y / 400);
            _pointerPos = e.GetPosition(this);
            return;
        }
        MeshViewWrap.Unwrap.Camera.LookRight(-(float)d.X / 400);
        MeshViewWrap.Unwrap.Camera.LookUp(-(float)d.Y / 400);
        _pointerPos = e.GetPosition(this);
        
    }
    
    private void UpdateBlocks(IndexedMesh3<float> mesh)
    {
        var centroid = mesh.VertexCentroid;
        var surfCentraid = mesh.SurfaceCentroid;
        var voluCentroid = mesh.VolumeCentroid;
        var volume = mesh.Volume;
        var area = mesh.SurfaceArea;
        CentroidBlock.Text = $"Centroids: Vertex [{centroid.X:N2}|{centroid.Y:N2}|{centroid.Z:N2}] Area [{surfCentraid.X:N2}|{surfCentraid.Y:N2}|{surfCentraid.Z:N2}] Volume [{voluCentroid.X:N2}|{voluCentroid.Y:N2}|{voluCentroid.Z:N2}] ";
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
        IndexedMesh3<float> mesh;
        try
        {
            await using var stream = await result[0].OpenReadAsync();
            var ifMesh = fileExt switch
            {
                ".stl" => FastStlReader.Read(stream, true),
                _ => throw new NotSupportedException()
            };
            mesh = ifMesh.Indexed();
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