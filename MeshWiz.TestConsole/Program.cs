using System.Diagnostics;
using System.Text;
using MeshWiz.Math;
using MeshWiz.Math.OpenCL;
using MeshWiz.OpenCL;
using MeshWiz.RefLinq;
using MeshWiz.Utility;
using OpenTK.Compute.OpenCL;

var mesh = new Sphere<float>(default, 1).Tessellate(4096, 4096);
using OclContext context = OclContext.Create(DeviceType.Gpu);
MathPrograms.AABB.ProgramContainer prog = MathPrograms.AABB.Create<Triangle3<float>, Vec3<float>, float>(context);
using var clProg = prog.Program;
using OclKernel indexedKernel = prog.CreateIndexed();

using OclBuffer<Vec3<float>> verts = context.CreateBuffer<Vec3<float>>(MemoryFlags.ReadOnly, mesh.Vertices.Length);
using OclBuffer<TriangleIndexer> indices = context.CreateBuffer<TriangleIndexer>(MemoryFlags.ReadOnly, mesh.Indices.Length);
using OclBuffer<AABB<Vec3<float>>> result = context.CreateBuffer<AABB<Vec3<float>>>(MemoryFlags.WriteOnly | MemoryFlags.HostReadOnly, mesh.Count);

var argMap = indexedKernel.ArgMap;
argMap[nameof(indices)].Set(indices);
argMap[nameof(verts)].Set(verts);
argMap[nameof(result)].Set(result);

using OclQueueManager queue= context.CreateCommandQueue().Select(OclHelper.Managed);

var sw = Stopwatch.StartNew();
// OclEvent res=verts.WriteNonBlocking(queue,mesh.Vertices);
// OclEvent indRes =indices.WriteNonBlocking(queue,mesh.Indices);
await verts.WriteAsync(queue, mesh.Vertices);
await indices.WriteAsync(queue, mesh.Indices);

// OclEvent execRes = indexedKernel.Run(queue, (nuint)mesh.Count);
var execRes = await indexedKernel.RunAsync(queue, (nuint)mesh.Count);

AABB<Vec3<float>>[] clBounds = await result.ReadAsync(queue);
// OclEvent resRead=result.ReadNonBlocking(queue, out var clBounds);
// var finRes=queue.Finish();
// if (!finRes)
//     throw new Exception(finRes.Info.ToString());
var clTime = sw.Elapsed;

sw.Restart();
var cpuBounds = mesh.Select(t => t.BBox).ToArray();
var cpuTime = sw.Elapsed;
var totalBytes = indices.ByteSize + verts.ByteSize + result.ByteSize;
Console.WriteLine($"GPU GiBYTE Size: {((uint)totalBytes)/1024f/1024f/1024f}");
Console.WriteLine($"CPUTIME {cpuTime} --> GPUTIME {clTime}");
Console.WriteLine($"Success: {clBounds.SequenceEqual(cpuBounds)}");

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