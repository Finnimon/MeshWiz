using System.Runtime.CompilerServices;
using System.Text;
using MeshWiz.Utility;
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

    public static OclDevice Create(CLDevice lowLevel) => lowLevel;
    public Result<CLResultCode,int> MaxComputeUnits => GetMaxComputeUnits(this);
    public Result<CLResultCode,string> Name => GetName(this);
    public Result<CLResultCode,int> VendorId => GetVendorId(this);
    public Result<CLResultCode,DeviceType> Type => GetDeviceType(this);
    public Result<CLResultCode,string> Version => GetVersion(this);
    public Result<CLResultCode,string> OclVersion => GetOclCVersion(this);

    public static Result<CLResultCode,string> GetOclCVersion(CLDevice device)
        =>GetInfo(device,DeviceInfo.OpenClCVersion).Select(Encoding.ASCII.GetString);

    public static Result<CLResultCode,string> GetVersion(CLDevice obj)
        =>GetInfo(obj,DeviceInfo.Version).Select(Encoding.ASCII.GetString);


    public static Result<CLResultCode,DeviceType> GetDeviceType(CLDevice obj)
        => GetInfo(obj,DeviceInfo.Type).Select(b=>Unsafe.As<byte, DeviceType>(ref b[0]));


    public static Result<CLResultCode,int> GetVendorId(CLDevice obj) 
        => GetInfo(obj,DeviceInfo.VendorId).Select(b=>BitConverter.ToInt32(b));

    public static Result<CLResultCode,int> GetMaxComputeUnits(CLDevice obj)
        => GetInfo(obj,DeviceInfo.MaximumComputeUnits).Select(b=>BitConverter.ToInt32(b));


    public static Result<CLResultCode,string> GetName(CLDevice obj)
    =>GetInfo(obj,DeviceInfo.Name).Select(Encoding.ASCII.GetString);

    public static Result<CLResultCode, byte[]> GetInfo(CLDevice obj, DeviceInfo target)
        => CL.GetDeviceInfo(obj, target, out var dat).AsResult(dat);
    

    /// <inheritdoc />
    public void Dispose() => CL.ReleaseDevice(this).LogError();

    public Result<CLResultCode> Retain() => CL.RetainDevice(this);
}