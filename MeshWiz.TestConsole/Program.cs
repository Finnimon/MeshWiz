using System.Runtime.Intrinsics;
using System.Text;
using MeshWiz.Abstraction.OpenCL;
using MeshWiz.Math;
using MeshWiz.Utility.Extensions;
using OpenTK.Compute.OpenCL;

Console.WriteLine("Hello World!");
Console.WriteLine(Vector128.IsHardwareAccelerated);
var platformIds = new CLPlatform[64];
// var platformResult= CL.GetPlatformIds(64U, platformIds,out var numberOfPlatforms);
//
// platformIds=platformIds[..(int)numberOfPlatforms];
// Console.WriteLine(platformResult);
// if (platformIds is not { Length: > 0 }) return;
// List<(CLPlatform platform, CLDevice device)> devices = [];
// foreach (var platformId in platformIds)
// {
//     var platformDesc = CL.GetPlatformInfo(platformId, PlatformInfo.Name, out var platformNameBytes);
//     var platformName=platformNameBytes is null?"Not found":Encoding.ASCII.GetString(platformNameBytes);
//     Console.WriteLine($"Platform name: {platformName}");
//     var deviceResult = CL.GetDeviceIds(platformId,DeviceType.Gpu, out var deviceIds);
//     Console.WriteLine($"Device result: {deviceResult}");
//     if (deviceIds is not { Length: > 0 })
//     {
//         Console.WriteLine($"Bad dev ID");
//         continue;
//     }    
//     foreach (var deviceId in deviceIds)
//     {
//         var deviceNameResult= CL.GetDeviceInfo(deviceId,DeviceInfo.Name,out var deviceInfoBytes);
//         var deviceName=platformNameBytes is null?"Dev name Not found":Encoding.ASCII.GetString(deviceInfoBytes);
//         var devCompResult = CL.GetDeviceInfo(deviceId, DeviceInfo.MaximumComputeUnits, out var value);
//         Console.WriteLine($"Device name: {deviceName}");
//         var hex= value.Select(b => $"{b:x2}");
//         var intVal= BitConverter.ToInt32(value);
//         Console.WriteLine(string.Join("",hex));
//         Console.WriteLine(intVal);
//     }
//     deviceIds.ForEach(CL.ReleaseDevice);
// }
//
//
// var platforms=OclPlatform.GetAll();
// foreach (var oclPlatform in platforms)
// {
//     using var disp = oclPlatform;
//     Console.WriteLine($"{oclPlatform.Name}");
//     foreach (var device in oclPlatform.AllDevices)
//     {
//         using var deviceDisp = device;
//         Console.WriteLine($"\t{device.Name} - {device.MaxComputeUnits} - {device.Type} - {device.VendorId} - {device.Version} - {device.OclVersion}");
//         using var context = OclContext.FromDevices(device);
//         Console.WriteLine($"\t\t{context.Devices.Contains(deviceDisp)}");
//     }
// }
var circle=new Circle3<float>(Vector3<float>.Zero, Vector3<float>.UnitY, 1);
var rev=circle.Reversed();
Console.WriteLine(circle.TraverseByAngle(0));
Console.WriteLine(rev.TraverseByAngle(0));
