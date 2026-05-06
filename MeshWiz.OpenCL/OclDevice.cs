using System;
using System.Runtime.CompilerServices;
using MeshWiz.Utility;
using OpenTK.Compute.OpenCL;

namespace MeshWiz.OpenCL;

/// <summary>
/// Abstraction Layer for <see cref="CLDevice"/>
/// </summary>
/// <param name="Handle">Handle to the hardware device</param>
public readonly record struct OclDevice(nint Handle) : IDisposable
{
    public static implicit operator CLDevice(OclDevice oclDevice) => Unsafe.As<OclDevice, CLDevice>(ref oclDevice);
    public static implicit operator OclDevice(CLDevice device) => Unsafe.As<CLDevice, OclDevice>(ref device);
    public static implicit operator nint(OclDevice dev) => dev.Handle;

    public static OclDevice Create(CLDevice lowLevel) => lowLevel;
    public Result<OclResultCode, int> MaxComputeUnits => GetMaxComputeUnits(this);
    public Result<OclResultCode, string> Name => GetName(this);
    public Result<OclResultCode, int> VendorId => GetVendorId(this);
    public Result<OclResultCode, DeviceType> Type => GetDeviceType(this);
    public Result<OclResultCode, string> Version => GetVersion(this);
    public Result<OclResultCode, string> OclVersion => GetOclCVersion(this);
    public Result<OclResultCode, string> Extensions => GetExtensions(this);
    public bool Fp64Supported => Extensions.TryGetValue(out var ext) && ext.Contains("cl_khr_fp16");
    public bool Fp16Supported => Extensions.TryGetValue(out var ext) && ext.Contains("cl_khr_fp64");


    public static Result<OclResultCode, string> GetOclCVersion(CLDevice device)
        => GetInfo(device, DeviceInfo.OpenClCVersion).Select(OclHelper.GetCLString);

    public static Result<OclResultCode, string> GetVersion(CLDevice obj)
        => GetInfo(obj, DeviceInfo.Version).Select(OclHelper.GetCLString);


    public static Result<OclResultCode, DeviceType> GetDeviceType(CLDevice obj)
        => GetInfo(obj, DeviceInfo.Type).Select(b => Unsafe.As<byte, DeviceType>(ref b[0]));


    public static Result<OclResultCode, int> GetVendorId(CLDevice obj)
        => GetInfo(obj, DeviceInfo.VendorId).Select(b => BitConverter.ToInt32(b));

    public static Result<OclResultCode, int> GetMaxComputeUnits(CLDevice obj)
        => GetInfo(obj, DeviceInfo.MaximumComputeUnits).Select(b => BitConverter.ToInt32(b));


    public static Result<OclResultCode, string> GetName(CLDevice obj)
        => GetInfo(obj, DeviceInfo.Name).Select(OclHelper.GetCLString);

    public static Result<OclResultCode, byte[]> GetInfo(CLDevice obj, DeviceInfo target)
        => CL.GetDeviceInfo(obj, target, out var dat).AsResult(dat);

    public static Result<OclResultCode, string> GetExtensions(CLDevice obj) =>
        GetInfo(obj, DeviceInfo.Extensions).Select(OclHelper.GetCLString);


    /// <inheritdoc />
    public void Dispose() => CL.ReleaseDevice(this).LogError();

    public Result<OclResultCode> Retain() => (OclResultCode)CL.RetainDevice(this);
}