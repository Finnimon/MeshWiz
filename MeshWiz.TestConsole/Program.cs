using MeshWiz.Math;

Console.WriteLine("Hello World!");

var l1=new Line<Vector2<float>,float>(Vector2<float>.Zero,new(1,0.5f));
var l2=new Line<Vector2<float>,float>(Vector2<float>.NegativeOne,Vector2<float>.Zero );
var doIntersect= Line.TryIntersectOnSegment(l1,l2,out var result,out var b);
Console.WriteLine(doIntersect);
Console.WriteLine(result);
Console.WriteLine(b);