using System.Diagnostics;
using MeshWiz.Math;
using MeshWiz.Utility;

Console.WriteLine("Hello World!");


var mesh=Bvh.Mesh<float>.Sah(new Sphere<float>(default,1).Tessellate());
Console.WriteLine(mesh.Count);
Console.WriteLine(mesh.Depth);
var transformedMesh=new Bvh.TransformableMesh<float>(mesh);
    var sw = Stopwatch.StartNew();
for (var i = 0; i < 10000; i++)
{
    transformedMesh.Transform = Mat4x4<float>.CreateTranslation(Vec3<float>.UnitX * 5f);
    transformedMesh.Intersects(mesh);
    transformedMesh.Transform = Mat4x4<float>.CreateTranslation(Vec3<float>.UnitX * 0.5f);
    transformedMesh.Intersects(mesh);
}

Console.WriteLine(sw.Elapsed);
