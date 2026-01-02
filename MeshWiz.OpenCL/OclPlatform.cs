using System.Runtime.CompilerServices;
using System.Text;
using MeshWiz.Utility;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly record struct OclPlatform(IntPtr Handle) : IDisposable
{
    public static implicit operator OclPlatform(CLPlatform platform) => Unsafe.As<CLPlatform, OclPlatform>(ref platform);
    public static implicit operator CLPlatform(OclPlatform platform) => Unsafe.As<OclPlatform, CLPlatform>(ref platform);
    public static OclPlatform Create(CLPlatform lowLevel) => lowLevel;

    public Result<CLResultCode,OclDevice[]> Gpus => GetDevices(this, DeviceType.Gpu);
    public Result<CLResultCode,OclDevice[]> Cpus => GetDevices(this, DeviceType.Cpu);
    public Result<CLResultCode,OclDevice[]> AcceleratorDevices => GetDevices(this, DeviceType.Accelerator);
    public Result<CLResultCode,OclDevice[]> CustomDevices => GetDevices(this, DeviceType.Custom);
    public Result<CLResultCode,OclDevice[]> AllDevices => GetDevices(this, DeviceType.All);
    public Result<CLResultCode,string> Name => GetName(this);

    public static Result<CLResultCode,string> GetName(CLPlatform platform) =>
        GetInfo(platform, PlatformInfo.Name)
            .Select(OclHelper.GetCLString);

    public static Result<CLResultCode, byte[]> GetInfo(CLPlatform platform, PlatformInfo info)
        => CL.GetPlatformInfo(platform, info, out var data).AsResult(data);
    

    public static Result<CLResultCode,OclPlatform[]> GetAll()
    {
        return CL.GetPlatformIds(out var value).AsResult(value)
            .Select(dev => dev.Select(Create).ToArray());
    }

    public static Result<CLResultCode,OclDevice[]> GetDevices(CLPlatform obj, DeviceType deviceType)
    {
        var code= CL.GetDeviceIds(obj, deviceType, out var devices);
        if (code is CLResultCode.InvalidValue)
            return Array.Empty<OclDevice>();
        return code.AsResult(devices)
            .Select(d => d.Select(OclDevice.Create))
            .Select(Enumerable.ToArray);
    }

    /// <inheritdoc />
    public void Dispose() => CL.UnloadPlatformCompiler(this).LogError();
}