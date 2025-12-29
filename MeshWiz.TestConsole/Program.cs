using MeshWiz.Collections;
using MeshWiz.Math;
using MeshWiz.Math.Benchmark;
using MeshWiz.Math.Signals;
using MeshWiz.OpenCL;
using MeshWiz.Utility;
using OpenTK.Compute.OpenCL;

OclPlatform[] platforms=OclPlatform.GetAll();
foreach (var oclPlatform in platforms)
{
    using var disp = oclPlatform;
    Console.WriteLine($"{oclPlatform.Name}");
    foreach (var dev in oclPlatform.AllDevices.OrElse([]))
    {
        using var dispose = dev;
        Console.WriteLine($"\t{dev.Name}");
    }
}