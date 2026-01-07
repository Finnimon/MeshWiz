using System.Diagnostics;
using MeshWiz.Math;
using MeshWiz.Math.OpenCL;
using MeshWiz.OpenCL;
using MeshWiz.RefLinq;
using MeshWiz.Utility;
using OpenTK.Compute.OpenCL;

var mesh = new Sphere<float>(default, 1).Tessellate(4096, 4096);
var trisPacked = mesh.ToArray();
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
await verts.WriteAsync(queue, mesh.Vertices);
await indices.WriteAsync(queue, mesh.Indices);

var execRes = await indexedKernel.RunAsync(queue, (nuint)mesh.Count);

AABB<Vec3<float>>[] clBounds = await result.ReadAsync(queue);
var clTime = sw.Elapsed;
using OclKernel packed = prog.CreatePacked();
using OclBuffer<Triangle3<float>> packedBuf =
    context.CreateBuffer<Triangle3<float>>(MemoryFlags.ReadOnly | MemoryFlags.HostWriteOnly, mesh.Count);
argMap = packed.ArgMap;
argMap[nameof(verts)].Set(packedBuf);
argMap[nameof(result)].Set(result);
sw.Restart();
await packedBuf.WriteAsync(queue, trisPacked);
var res=await packed.RunAsync(queue, (nuint)mesh.Count);
if (!res)
    throw new Exception();
AABB<Vec3<float>>[] clBounds2 = await result.ReadAsync(queue);
var clTime2=sw.Elapsed;
sw.Restart();
var cpuBounds = trisPacked.Iterate().Select(t => t.BBox).ToArray();
var cpuTime = sw.Elapsed;
var totalBytes = indices.ByteSize + verts.ByteSize + result.ByteSize;
Console.WriteLine($"GPU GiBYTE Size: {((uint)totalBytes)/1024f/1024f/1024f}");
Console.WriteLine($"CPUTIME {cpuTime} --> GPUTIME {clTime} --> GPUTIME2 {clTime2}");
Console.WriteLine($"Success: {clBounds.SequenceEqual(cpuBounds)}");

