using Avalonia.Controls;
using MeshWiz.Abstraction.OpenTK;
using MeshWiz.Math;

namespace MeshWiz.UI.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var lv = LineViewWrapper0.Wrapped;
        lv.PolyLine = new PolyLine<Vector3<float>, float>([
        new(1,1,0),
        new(-1,1,0),
        new(-1,-1,0),
        new(1,-1,0),
        new(1,1,0),
        ]);
        var lv2 = LineViewWrapper1.Wrapped;
        lv2.PolyLine = new PolyLine<Vector3<float>, float>([
            new(2,2,0),
            new(-2,2,0),
            new(-2,-2,0),
            new(2,-2,0),
            new(2,2,0),
        ]);
        Console.WriteLine(lv2.PolyLine.Centroid);
        lv.Camera=lv2.Camera;
        lv2.Camera.UnitUp=Vector3<float>.UnitY;
        
    }
}