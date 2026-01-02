using System.Diagnostics;
using System.Text;
using MeshWiz.Math;
using MeshWiz.Math.OpenCL;
using MeshWiz.OpenCL;
using MeshWiz.RefLinq;
using OpenTK.Compute.OpenCL;

var mesh = new Sphere<float>(default,1).Tessellate(4096,4096);
var sw=Stopwatch.StartNew();
using OclContext context=OclContext.Create(DeviceType.Gpu);
Programs.AABB.ProgramContainer prog= Programs.AABB.Create<Triangle3<float>,Vec3<float>,float>(context);
using var clProg=prog.Program;
using OclKernel indexedKernel=prog.CreateIndexed();
using OclBuffer<Vec3<float>> verts = context.CreateBuffer<Vec3<float>>(MemoryFlags.ReadOnly,mesh.Vertices.Length);
using OclBuffer<TriangleIndexer> indices = context.CreateBuffer<TriangleIndexer>(MemoryFlags.ReadOnly,mesh.Indices.Length);
using OclBuffer<AABB<Vec3<float>>> result=context.CreateBuffer<AABB<Vec3<float>>>(MemoryFlags.WriteOnly|MemoryFlags.HostReadOnly,mesh.Count);
var argMap = indexedKernel.ArgMap;
argMap[nameof(indices)].Set(indices);
argMap[nameof(verts)].Set(verts);
argMap[nameof(result)].Set(result);
using OclCommandQueue queue=context.CreateCommandQueue();
await verts.WriteAsync(queue,mesh.Vertices);
await indices.WriteAsync(queue,mesh.Indices);
var execRes=await indexedKernel.RunAsync(queue,[(nuint)mesh.Count]);
var res=await result.ReadAsync(queue);
var clTime = sw.Elapsed;
var clBounds = res.Value;
sw.Restart();
var cpuBounds = mesh.Select(t => t.BBox).ToArray();
var cpuTime = sw.Elapsed;
Console.WriteLine($"{cpuTime} --> {clTime}");
Console.WriteLine(clBounds.SequenceEqual(cpuBounds));

//
// var underlying= Enum.GetValues<CLResultCode>();
// var sb=new StringBuilder();
// sb.AppendLine("public enum OclResultCode\n{");
// foreach (var clResultCode in underlying.OrderDescending())
// {
//     sb.AppendLine($"{clResultCode} = {(int)clResultCode},");
// }
// sb.AppendLine("}");
// Console.WriteLine(sb.ToString());