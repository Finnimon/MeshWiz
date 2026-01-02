using System.Diagnostics;
using MeshWiz.Math;
using MeshWiz.Math.OpenCL;
using MeshWiz.OpenCL;
using OpenTK.Compute.OpenCL;

var mesh = new Sphere<float>(Vec3<float>.Zero, 1).Tessellate(256,256);

using OclContext context=OclContext.Create(DeviceType.Gpu);
Programs.AABB.ProgramContainer prog= Programs.AABB.Create<float>(context,3,3);
using var clProg=prog.Program;

using OclKernel indexedKernel=prog.CreateIndexed();
using OclBuffer<Vec3<float>> verts = context.CreateBuffer<Vec3<float>>(MemoryFlags.ReadOnly,mesh.Vertices.Length);
using OclBuffer<TriangleIndexer> indices = context.CreateBuffer<TriangleIndexer>(MemoryFlags.ReadOnly,mesh.Indices.Length);
using OclBuffer<AABB<Vec3<float>>> result=context.CreateBuffer<AABB<Vec3<float>>>(MemoryFlags.WriteOnly|MemoryFlags.HostReadOnly,mesh.Count);
var kArgs = indexedKernel.Arguments;
indexedKernel[nameof(indices)].Set(indices).Info.ThrowOnError();
indexedKernel[nameof(verts)].Set(verts).Info.ThrowOnError();
indexedKernel[nameof(result)].Set(result).Info.ThrowOnError();
using OclCommandQueue queue=context.CreateCommandQueue();
await verts.WriteAsync(queue,mesh.Vertices);
await indices.WriteAsync(queue,mesh.Indices);
var sw=Stopwatch.StartNew();
var execRes=await indexedKernel.RunAsync(queue,[(nuint)mesh.Count]);
var res=await result.ReadAsync(queue);
var clTime = sw.Elapsed;
var clBounds = res.Value;
sw.Restart();
var cpuBounds = mesh.Select(t => t.BBox).ToArray();
var cpuTime = sw.Elapsed;
Console.WriteLine($"{cpuTime} --> {clTime}");
// Console.WriteLine(clBounds.SequenceEqual(cpuBounds));


// Console.WriteLine(string.Join("\n",clBounds.Zip(cpuBounds).Select((a,b)=>$"{b}  CLMAX{a.First.Max} CPUMAX{a.Second.Max} - CLMIN{a.First.Min} CPUMIN{a.Second.Min}")));
