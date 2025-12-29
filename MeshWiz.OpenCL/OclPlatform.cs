using System.Runtime.CompilerServices;
using System.Text;
using MeshWiz.Contracts;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

public readonly record struct OclPlatform(IntPtr Handle) : IAbstraction<OclPlatform, CLPlatform>, IDisposable
{
    public static implicit operator OclPlatform(CLPlatform platform) => Unsafe.As<CLPlatform, OclPlatform>(ref platform);
    public static implicit operator CLPlatform(OclPlatform platform) => Unsafe.As<OclPlatform, CLPlatform>(ref platform);
    public static OclPlatform Abstract(CLPlatform lowLevel) => lowLevel;
    public static CLPlatform LowLevel(OclPlatform highLevel) => highLevel;

    public OclDevice[] Gpus => GetDevices(this, DeviceType.Gpu);
    public OclDevice[] Cpus => GetDevices(this, DeviceType.Cpu);
    public OclDevice[] AcceleratorDevices => GetDevices(this, DeviceType.Accelerator);
    public OclDevice[] CustomDevices => GetDevices(this, DeviceType.Custom);
    public OclDevice[] AllDevices => GetDevices(this, DeviceType.All);
    public string Name => GetName(this);

    public static string GetName(CLPlatform platform)
    {
        CL.GetPlatformInfo(platform, PlatformInfo.Name, out var info)
            .ThrowOnError();
        return Encoding.ASCII.GetString(info!);
    }

    public static OclPlatform[] GetAll()
    {
        CL.GetPlatformIds(out var value).ThrowOnError();
        _ = value ?? throw new NullReferenceException();
        return value.Select(Abstract).ToArray();
    }

    private OclDevice[] GetDevices(CLPlatform obj, DeviceType deviceType)
    {
        CL.GetDeviceIds(obj, deviceType, out var devices).ThrowOnError(ignore: CLResultCode.InvalidValue);
        devices ??= [];
        return devices.Select(OclDevice.Abstract).ToArray();
    }

    /// <inheritdoc />
    public void Dispose() => CL.UnloadPlatformCompiler(this).LogError();
}