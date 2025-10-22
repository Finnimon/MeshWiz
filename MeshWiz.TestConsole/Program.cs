using System.Numerics;
using System.Runtime.Intrinsics;using MeshWiz.Abstraction.OpenCL;
using MeshWiz.Math;
using OpenTK.Compute.OpenCL;

// Console.WriteLine("Hello World!");
// Console.WriteLine(Vector128.IsHardwareAccelerated);
// var circle=new Circle3<float>(Vector3<float>.Zero, Vector3<float>.UnitY, 1);
// var arcSection= circle.ArcSection(0, 3.14f);
// Console.WriteLine(arcSection.ToPolyline());

foreach (var pl in OclPlatform.GetAll())
{
    using var dispo = pl;
    Console.WriteLine($"Platform: {dispo.Name}");
    foreach (var device in pl.AllDevices)
    {
        using var disp2 = device;
        Console.WriteLine($"\tDevice: \n\t\t{device.Name} \n\t\t{device.MaxComputeUnits}" +
                          $"\n\t\t{device.Version}" +
                          $"\n\t\t{device.OclVersion}");
    }
}