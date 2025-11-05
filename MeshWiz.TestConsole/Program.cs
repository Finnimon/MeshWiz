using System.Diagnostics;
using MeshWiz.Math;
using MeshWiz.Results;
using MeshWiz.Utility;

var cone = new Cone<float>((-Vector3<float>.UnitY).LineTo(Vector3<float>.UnitY), 0.5f);
_= Numbers<float>.TwoPi;
_ = Numbers<Vector2<float>>.TwoPi; 
_ = Numbers<Vector3<float>>.TwoPi; 
Vector3<float> p1 = new(-10, -10, -10);
var start = cone.ClampToSurface(p1);
var coneGeodesic = ConeGeodesic<float>.FromDirection(in cone, start, new(0, 2, 1));
var geodesic = coneGeodesic.ToPolyline();

// var coneGeodesic =
//     ConeGeodesic<float>.BetweenPoints(in cone,start,end);
var swGeod = Stopwatch.StartNew();
coneGeodesic = ConeGeodesic<float>.FromDirection(in cone, start, new(0, 2, 1));
var createTime = swGeod.Elapsed.TotalMilliseconds;
swGeod.Restart();
geodesic = coneGeodesic.ToPolyline();
var polyTime = swGeod.Elapsed.TotalMilliseconds;
swGeod.Stop();
Console.WriteLine($"Create: {createTime:F2}ms Poly: {polyTime:F2}ms");
var res=Result<string,StringInfo>.Failure(StringInfo.Failure);
 res=Result<string,StringInfo>.Success("Somestring");
var r=res.Value;

public enum StringInfo
{
    Success=0,
    Failure=1,
    TypeFailure=2,
    FileAccessViolation=3,
    FileNotFound=4,
    PathNotFound=5,
}