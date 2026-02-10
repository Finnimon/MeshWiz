using System.Diagnostics;
using MeshWiz.Math;

Console.WriteLine("Hello World!");


var arc = new Arc2<double>(default, 1, 0, double.Pi);
var poly = arc.ToPolyline();
BSpline<Vec2<double>, double> spline = new(poly.Vertices);
spline.Traverse(0.5);
spline.Traverse(0.5);
spline.Traverse(0.5);
spline.Traverse(0.5);
for (var i = 0; i < 1000; i++)
{
    var t=i/1000.0;
    spline.Traverse(t);
}
var sw = Stopwatch.StartNew();
for (var i = 0; i < 10000; i++)
{
    var t=i/10000.0;
    spline.Traverse(t);
    // Bezier.Lerp<Vec2<double>>(spline.Knots,Vec2<double>.Create(t));
}
Console.WriteLine(spline.Knots.Length);
Console.WriteLine(sw.Elapsed);
Console.WriteLine(sw.Elapsed/1000);
