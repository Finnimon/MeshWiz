using System.Runtime.CompilerServices;
using System.Text;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

/// <summary>
/// Abstraction Layer for <see cref="CLDevice"/>
/// </summary>
/// <param name="Handle">Handle to the hardware device</param>
public readonly record struct OclDevice(IntPtr Handle) : IDisposable
{
    public static implicit operator CLDevice(OclDevice oclDevice) => Unsafe.As<OclDevice, CLDevice>(ref oclDevice);
    public static implicit operator OclDevice(CLDevice device) => Unsafe.As<CLDevice, OclDevice>(ref device);

    /// <inheritdoc />
    public static OclDevice Abstract(CLDevice lowLevel) => lowLevel;

    /// <inheritdoc />
    public static CLDevice LowLevel(OclDevice highLevel) => highLevel;

    public int MaxComputeUnits => GetMaxComputeUnits(this);
    public string Name => GetName(this);
    public int VendorId => GetVendorId(this);
    public DeviceType Type => GetDeviceType(this);
    public string Version => GetVersion(this);
    public string OclVersion => GetOclCVersion(this);

    public static string GetOclCVersion(CLDevice device)
    {
        CL.GetDeviceInfo(device, DeviceInfo.OpenClCVersion, out var value).ThrowOnError();
        return Encoding.ASCII.GetString(value!);
    }

    private string GetVersion(CLDevice obj)
    {
        CL.GetDeviceInfo(obj, DeviceInfo.Version, out var result).ThrowOnError();
        return Encoding.ASCII.GetString(result!);
    }


    public static DeviceType GetDeviceType(CLDevice obj)
    {
        CL.GetDeviceInfo(obj, DeviceInfo.Type, out var value).ThrowOnError();
        return Unsafe.As<byte, DeviceType>(ref value![0]);
    }

    public static int GetVendorId(CLDevice obj)
    {
        CL.GetDeviceInfo(obj, DeviceInfo.VendorId, out var value).ThrowOnError();
        return Unsafe.As<byte, int>(ref value![0]);
    }

    public static int GetMaxComputeUnits(CLDevice obj)
    {
        CL.GetDeviceInfo(obj, DeviceInfo.MaximumComputeUnits, out var value).ThrowOnError();
        return BitConverter.ToInt32(value!);
    }

    public static string GetName(CLDevice obj)
    {
        CL.GetDeviceInfo(obj, DeviceInfo.Name, out var value)
            .ThrowOnError();
        return Encoding.ASCII.GetString(value!);
    }


    /// <inheritdoc />
    public void Dispose() => CL.ReleaseDevice(this).LogError();

    public void Retain() => CL.RetainDevice(this);
}