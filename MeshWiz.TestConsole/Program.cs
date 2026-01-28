using System.Diagnostics;
using CommunityToolkit.Diagnostics;
using MeshWiz.Collections;
using MeshWiz.Math;
using MeshWiz.Math.OpenCL;
using MeshWiz.OpenCL;
using MeshWiz.RefLinq;
using MeshWiz.Utility;
using OpenTK.Compute.OpenCL;
using Vec2d = MeshWiz.Math.Vec2<double>;


// var circle = new Circle2<double>(Vec2d.Zero, 1).ToPolyline(new PolylineTessellationParameter<double> { MaxAngularDeviation = double.Pi * 0.001 });
// var sw = Stopwatch.StartNew();
// var bvhCircle = BvhPolyline<Vec2d, double>.BinaryBalanced(circle);
// Console.WriteLine(sw.Elapsed);
//
// var bounds = bvhCircle.BBox;
// var line = bounds.Min.LineTo(AABB.UpperLeft(bounds));
// var meanderDir = line.Direction.Left;
// line = new(line.Start - meanderDir, line.End - meanderDir);
// RollingList<double> buf = [];
//
// List<Vec3<double>> meanderInfill = [];
// sw.Restart();
// var countNow = 10000000;
// for (var i = 1; i < countNow+1; i++)
// {
//     var origin = line.Traverse(i / (countNow+1.0));
//     var ray = Ray2<double>.CreateUnsafe(origin, meanderDir);
//     var trav = new BvhPolyline.AllHits<double>(ray, buf);
//     var hit = bvhCircle.TraverseBvh<BvhPolyline.AllHits<double>, double>(trav);
//     if(!hit)
//         ThrowHelper.ThrowInvalidOperationException("Failed to hit sure hit");
//     var max = buf.Max();
//     var min = buf.Min();
//     buf.Clear();
//     // var reverse = (i & 1) == 1;
//     // var (start, end) = reverse ? (max, min) : (min, max);
//     // meanderInfill.Add(Vec3<double>.Create(ray.Traverse(start),0));
// }
//
// var el = sw.Elapsed;
// Console.WriteLine(el);
// Console.WriteLine(el.TotalNanoseconds/countNow);
//

foreach (var oclPlatform in OclPlatform.GetAll().Value)
{
    Console.WriteLine($"Platform: {oclPlatform.Name}");
    foreach (var oclDevice in oclPlatform.AllDevices.Value)
    {
        Console.WriteLine($"Device: {oclDevice.Name}");
        Console.WriteLine($"Device: {oclDevice.OclVersion}");
        oclDevice.Dispose();
    }
    oclPlatform.Dispose();
}
// var mesh = new Sphere<float>(default, 1).Tessellate(4096, 4096);
// var trisPacked = mesh.ToArray();
// using OclContext context = OclContext.Create(DeviceType.Gpu);
// MathPrograms.AABB.ProgramContainer prog = MathPrograms.AABB.Create<Triangle3<float>, Vec3<float>, float>(context);
// using var clProg = prog.Program;
// using OclKernel indexedKernel = prog.CreateIndexed();
//
// using OclBuffer<Vec3<float>> verts = context.CreateBuffer<Vec3<float>>(MemoryFlags.ReadOnly, mesh.Vertices.Length);
// using OclBuffer<TriangleIndexer> indices = context.CreateBuffer<TriangleIndexer>(MemoryFlags.ReadOnly, mesh.Indices.Length);
// using OclBuffer<AABB<Vec3<float>>> result = context.CreateBuffer<AABB<Vec3<float>>>(MemoryFlags.WriteOnly | MemoryFlags.HostReadOnly, mesh.Count);
//
// var argMap = indexedKernel.ArgMap;
// argMap[nameof(indices)].Set(indices);
// argMap[nameof(verts)].Set(verts);
// argMap[nameof(result)].Set(result);
//
// using OclQueueManager queue= context.CreateCommandQueue().Select(OclHelper.Managed);
//
// var sw = Stopwatch.StartNew();
// await verts.WriteAsync(queue, mesh.Vertices);
// await indices.WriteAsync(queue, mesh.Indices);
//
// var execRes = await indexedKernel.RunAsync(queue, (nuint)mesh.Count);
//
// AABB<Vec3<float>>[] clBounds = await result.ReadAsync(queue);
// var clTime = sw.Elapsed;
// using OclKernel packed = prog.CreatePacked();
// using OclBuffer<Triangle3<float>> packedBuf =
//     context.CreateBuffer<Triangle3<float>>(MemoryFlags.ReadOnly | MemoryFlags.HostWriteOnly, mesh.Count);
// argMap = packed.ArgMap;
// argMap[nameof(verts)].Set(packedBuf);
// argMap[nameof(result)].Set(result);
// sw.Restart();
// await packedBuf.WriteAsync(queue, trisPacked);
// var res=await packed.RunAsync(queue, (nuint)mesh.Count);
// if (!res)
//     throw new Exception();
// AABB<Vec3<float>>[] clBounds2 = await result.ReadAsync(queue);
// var clTime2=sw.Elapsed;
// sw.Restart();
// var cpuBounds = trisPacked.Iterate().Select(t => t.BBox).ToArray();
// var cpuTime = sw.Elapsed;
// var totalBytes = indices.ByteSize + verts.ByteSize + result.ByteSize;
// Console.WriteLine($"GPU GiBYTE Size: {((uint)totalBytes)/1024f/1024f/1024f:N4}");
// Console.WriteLine($"CPUTIME {cpuTime} --> GPUTIME {clTime} --> GPUTIME2 {clTime2}");
// Console.WriteLine($"Success: {clBounds.SequenceEqual(cpuBounds)}");
//